import { test, expect } from "@playwright/test";
import AxeBuilder from "@axe-core/playwright";

const PAGES = [
  { path: "/", name: "Home" },
  { path: "/about", name: "About" },
  { path: "/contact", name: "Contact" },
];

test.describe("Automated Accessibility Audit", () => {
  for (const { path, name } of PAGES) {
    test(`${name} (${path}) has no critical a11y violations`, async ({ page }) => {
      await page.goto(path, { waitUntil: "networkidle" });
      await page.waitForLoadState("domcontentloaded");

      const results = await new AxeBuilder({ page })
        .withTags(["wcag2a", "wcag2aa", "wcag21a", "wcag21aa", "best-practice"])
        .analyze();

      const critical = results.violations.filter(
        (v) => v.impact === "critical" || v.impact === "serious"
      );

      if (critical.length > 0) {
        console.log(`\n=== ${name} (${path}): ${critical.length} critical/serious violations ===`);
        for (const v of critical) {
          console.log(`  [${v.impact}] ${v.id}: ${v.description}`);
          for (const node of v.nodes) {
            console.log(`    → ${node.html}`);
            for (const check of node.all) {
              if (!check.passed) console.log(`      ✗ ${check.message}`);
            }
          }
        }
      }

      expect(critical.length).toBe(0);
    });
  }
});

test.describe("Visual Contrast & Touch Target Audit", () => {
  test("Home page touch targets meet minimum size", async ({ page }) => {
    await page.goto("/", { waitUntil: "networkidle" });
    const smallTargets = await page.evaluate(() => {
      const interactive = document.querySelectorAll(
        'button, a, input, select, textarea, [role="button"], [role="link"]'
      );
      const small: { tag: string; text: string; size: string }[] = [];
      interactive.forEach((el) => {
        const rect = el.getBoundingClientRect();
        const w = rect.width;
        const h = rect.height;
        if (w < 44 || h < 44) {
          const text =
            el.textContent?.trim().slice(0, 30) ||
            (el as HTMLElement).ariaLabel ||
            el.tagName;
          small.push({ tag: el.tagName, text, size: `${Math.round(w)}x${Math.round(h)}` });
        }
      });
      return small;
    });

    if (smallTargets.length > 0) {
      console.log(`\n=== Touch targets under 44px on Home page: ${smallTargets.length} ===`);
      for (const t of smallTargets) {
        console.log(`  ${t.tag}: "${t.text}" (${t.size})`);
      }
    }

    expect(smallTargets.length).toBeLessThanOrEqual(5);
  });
});
