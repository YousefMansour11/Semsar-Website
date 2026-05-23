import { describe, it, expect, beforeEach, vi } from "vitest";
import { captureUtm, trackPageView, getTrackingFields, initTracker } from "../lib/tracker";

const TRACKER_KEY = "semsar_tracker";

function setSearchParams(queryString: string) {
  window.location = new URL(`http://localhost:3000${queryString}`) as unknown as Location;
}

function setReferrer(url: string) {
  Object.defineProperty(document, "referrer", { value: url, configurable: true });
}

function setUserAgent(ua: string) {
  Object.defineProperty(navigator, "userAgent", { value: ua, configurable: true });
}

function setTitle(title: string) {
  document.title = title;
}

function advanceMs(ms: number) {
  vi.advanceTimersByTime(ms);
}

beforeEach(() => {
  localStorage.clear();
  setSearchParams("");
  setReferrer("");
  setUserAgent("Mozilla/5.0 TestAgent");
  setTitle("Semsar");
  vi.useFakeTimers();
  vi.setSystemTime(new Date("2025-06-15T12:00:00Z"));
});

// ============================================================
// PART 1 — UTM Capture & Storage
// ============================================================
describe("PART 1: UTM Capture", () => {
  it("1a. parses UTM parameters correctly", () => {
    setSearchParams("?utm_source=tiktok&utm_campaign=test1&utm_medium=cpc");
    captureUtm();

    const raw = localStorage.getItem(TRACKER_KEY);
    expect(raw).not.toBeNull();
    const data = JSON.parse(raw!);
    expect(data.source).toBe("tiktok");
    expect(data.campaign).toBe("test1");
    expect(data.medium).toBe("cpc");
  });

  it("1b. normalizes values (lowercase, trimmed)", () => {
    setSearchParams("?utm_source= TikTok &utm_campaign= TestCampaign &utm_medium= CPC ");
    captureUtm();

    const raw = localStorage.getItem(TRACKER_KEY);
    const data = JSON.parse(raw!);
    expect(data.source).toBe("tiktok");
    expect(data.campaign).toBe("testcampaign");
    expect(data.medium).toBe("cpc");
  });

  it("1c. stores all optional UTM fields (content, term)", () => {
    setSearchParams("?utm_source=google&utm_campaign=summer&utm_medium=cpc&utm_content=banner1&utm_term=house");
    captureUtm();

    const raw = localStorage.getItem(TRACKER_KEY);
    const data = JSON.parse(raw!);
    expect(data.content).toBe("banner1");
    expect(data.term).toBe("house");
  });

  it("1d. stores landingPage = current URL path + search", () => {
    setSearchParams("/properties/123?utm_source=fb&utm_campaign=spring&utm_medium=social");
    captureUtm();

    const raw = localStorage.getItem(TRACKER_KEY);
    const data = JSON.parse(raw!);
    expect(data.landingPage).toBe("/properties/123?utm_source=fb&utm_campaign=spring&utm_medium=social");
  });

  it("1e. does nothing when no utm_source in URL (captureUtm only)", () => {
    setSearchParams("?foo=bar");
    captureUtm();

    const raw = localStorage.getItem(TRACKER_KEY);
    expect(raw).toBeNull();
  });

  it("1f. does nothing when no query string at all (captureUtm only)", () => {
    setSearchParams("");
    captureUtm();

    const raw = localStorage.getItem(TRACKER_KEY);
    expect(raw).toBeNull();
  });
});

// ============================================================
// PART 2 — InitTracker (all visitors)
// ============================================================
describe("PART 2: initTracker", () => {
  it("2a. initTracker creates tracker for direct visitors", () => {
    initTracker();

    const raw = localStorage.getItem(TRACKER_KEY);
    expect(raw).not.toBeNull();
    const data = JSON.parse(raw!);
    expect(data.source).toBe("direct");
    expect(data.landingPage).toBe("/");
    expect(data.sessionStartAt).toBeDefined();
  });

  it("2b. initTracker creates tracker with UTM params", () => {
    setSearchParams("?utm_source=facebook&utm_campaign=ad&utm_medium=social");
    initTracker();

    const data = JSON.parse(localStorage.getItem(TRACKER_KEY)!);
    expect(data.source).toBe("facebook");
    expect(data.campaign).toBe("ad");
  });

  it("2c. initTracker does not overwrite existing data", () => {
    setSearchParams("?utm_source=first&utm_campaign=original&utm_medium=cpc");
    captureUtm();

    setSearchParams("?utm_source=second&utm_campaign=new&utm_medium=social");
    initTracker();

    const data = JSON.parse(localStorage.getItem(TRACKER_KEY)!);
    expect(data.source).toBe("first");
    expect(data.campaign).toBe("original");
  });

  it("2d. initTracker initializes sessionStartAt", () => {
    initTracker();

    const data = JSON.parse(localStorage.getItem(TRACKER_KEY)!);
    expect(data.sessionStartAt).toBe(data.firstVisitAt);
  });
});

// ============================================================
// PART 3 — Persistence
// ============================================================
describe("PART 3: Persistence", () => {
  it("3a. data persists on reload without UTM", () => {
    setSearchParams("?utm_source=twitter&utm_campaign=promo&utm_medium=social");
    captureUtm();

    setSearchParams("");
    const fields = getTrackingFields();
    expect(fields.source).toBe("twitter");
    expect(fields.campaign).toBe("promo");
  });

  it("3b. getTrackingFields returns fallback when no stored data", () => {
    const fields = getTrackingFields();
    expect(fields.source).toBe("direct");
    expect(fields.pageViews).toBe(0);
  });

  it("3c. direct visitor data persists after initTracker", () => {
    initTracker();
    setSearchParams("/about");
    trackPageView();

    const fields = getTrackingFields();
    expect(fields.source).toBe("direct");
    expect(fields.pageViews).toBe(1);
  });
});

// ============================================================
// PART 4 — Expiration
// ============================================================
describe("PART 4: Expiration", () => {
  it("4a. expired data is cleared and returns 'direct'", () => {
    initTracker();
    setSearchParams("?utm_source=google&utm_campaign=old&utm_medium=cpc");
    captureUtm();

    advanceMs(8 * 24 * 60 * 60 * 1000);

    const fields = getTrackingFields();
    expect(fields.source).toBe("direct");
    expect(fields.pageViews).toBe(0);
  });

  it("4b. expired data is cleaned on read", () => {
    setSearchParams("?utm_source=google&utm_campaign=old&utm_medium=cpc");
    captureUtm();

    advanceMs(8 * 24 * 60 * 60 * 1000);

    getTrackingFields();
    expect(localStorage.getItem(TRACKER_KEY)).toBeNull();
  });

  it("4c. new UTMs captured after expiration", () => {
    setSearchParams("?utm_source=google&utm_campaign=old&utm_medium=cpc");
    captureUtm();

    advanceMs(8 * 24 * 60 * 60 * 1000);

    setSearchParams("?utm_source=tiktok&utm_campaign=new&utm_medium=social");
    captureUtm();

    const raw = localStorage.getItem(TRACKER_KEY);
    const data = JSON.parse(raw!);
    expect(data.source).toBe("tiktok");
    expect(data.campaign).toBe("new");
  });
});

// ============================================================
// PART 5 — Route Tracking
// ============================================================
describe("PART 5: Route Tracking", () => {
  it("5a. increments pageViews on each call", () => {
    initTracker();

    setSearchParams("/");
    trackPageView();
    expect(getTrackingFields().pageViews).toBe(1);

    setSearchParams("/properties/123");
    trackPageView();
    expect(getTrackingFields().pageViews).toBe(2);

    setSearchParams("/contact");
    trackPageView();
    expect(getTrackingFields().pageViews).toBe(3);
  });

  it("5b. visitHistory logs path and title", () => {
    initTracker();
    setTitle("Home");
    setSearchParams("/");
    trackPageView();

    setTitle("Property");
    setSearchParams("/properties/123");
    trackPageView();

    const fields = getTrackingFields();
    const history = JSON.parse(fields.visitHistory!);
    expect(history).toHaveLength(2);
    expect(history[0].path).toBe("/");
    expect(history[0].title).toBe("Home");
    expect(history[1].path).toBe("/properties/123");
    expect(history[1].title).toBe("Property");
  });

  it("5c. visitHistory timestamps are valid ISO strings", () => {
    initTracker();
    setSearchParams("/");
    trackPageView();

    const history = JSON.parse(getTrackingFields().visitHistory!);
    const ts = new Date(history[0].timestamp);
    expect(ts.toISOString()).toBe(history[0].timestamp);
  });

  it("5d. lastReferrer updates from document.referrer", () => {
    setReferrer("https://facebook.com/page");
    initTracker();

    setReferrer("https://twitter.com/post");
    setSearchParams("/properties/123");
    trackPageView();

    expect(getTrackingFields().lastReferrer).toBe("https://twitter.com/post");
  });

  it("5e. visitHistory capped at 15 entries", () => {
    initTracker();

    for (let i = 0; i < 20; i++) {
      setSearchParams(`/page${i}`);
      trackPageView();
    }

    const history = JSON.parse(getTrackingFields().visitHistory!);
    expect(history).toHaveLength(15);
    expect(history[0].path).toBe("/page5");
    expect(history[14].path).toBe("/page19");
  });

  it("5f. trackPageView does nothing when no tracker data", () => {
    localStorage.clear();
    setSearchParams("/some-page");
    trackPageView();

    const fields = getTrackingFields();
    expect(fields.source).toBe("direct");
    expect(fields.pageViews).toBe(0);
    expect(fields.visitHistory).toBeUndefined();
  });
});

// ============================================================
// PART 6 — Session Duration
// ============================================================
describe("PART 6: Session Duration", () => {
  it("6a. sessionDuration > 0 after time passes", () => {
    initTracker();
    advanceMs(45_000);

    const fields = getTrackingFields();
    expect(fields.sessionDuration!).toBeGreaterThan(0);
    expect(fields.sessionDuration!).toBeGreaterThanOrEqual(45);
  });

  it("6b. sessionDuration reflects elapsed from sessionStartAt", () => {
    initTracker();
    advanceMs(60_000);
    expect(getTrackingFields().sessionDuration).toBe(60);

    advanceMs(120_000);
    expect(getTrackingFields().sessionDuration).toBe(180);
  });

  it("6c. sessionDuration is undefined when no tracker data", () => {
    localStorage.clear();
    const fields = getTrackingFields();
    expect(fields.sessionDuration).toBeUndefined();
  });

  it("6d. session resets after 30+ min idle gap", () => {
    initTracker();
    setSearchParams("/");
    trackPageView();
    advanceMs(60_000);

    advanceMs(31 * 60 * 1000);
    setSearchParams("/after-idle");
    trackPageView();

    const fields = getTrackingFields();
    expect(fields.sessionDuration).toBeLessThan(120);
    expect(fields.pageViews).toBe(2);
  });

  it("6e. session does NOT reset within 30 min", () => {
    initTracker();
    advanceMs(60_000);
    setSearchParams("/page1");
    trackPageView();
    advanceMs(15 * 60 * 1000);
    setSearchParams("/page2");
    trackPageView();

    const fields = getTrackingFields();
    expect(fields.sessionDuration).toBeGreaterThanOrEqual(16 * 60);
  });
});

// ============================================================
// PART 7 — Form Payload
// ============================================================
describe("PART 7: Form Payload (getTrackingFields)", () => {
  it("7a. returns all fields when tracker exists", () => {
    setSearchParams("?utm_source=fb&utm_campaign=ads&utm_medium=social&utm_content=pic&utm_term=land");
    setReferrer("https://facebook.com");
    captureUtm();

    setSearchParams("/contact");
    trackPageView();

    const fields = getTrackingFields();
    expect(fields.source).toBe("fb");
    expect(fields.medium).toBe("social");
    expect(fields.campaign).toBe("ads");
    expect(fields.content).toBe("pic");
    expect(fields.term).toBe("land");
    expect(fields.landingPage).toContain("utm_source=fb");
    expect(fields.firstVisitAt).toBeDefined();
    expect(fields.currentPage).toBe("/contact");
    expect(fields.userAgent).toBe("Mozilla/5.0 TestAgent");
    expect(fields.pageViews).toBe(1);
    expect(fields.sessionDuration).toBeGreaterThanOrEqual(0);
    expect(fields.lastReferrer).toBeDefined();
    expect(fields.visitHistory).toBeDefined();
  });

  it("7b. fallback: source=direct when no tracker", () => {
    localStorage.clear();
    const fields = getTrackingFields();
    expect(fields.source).toBe("direct");
    expect(fields.medium).toBeUndefined();
    expect(fields.campaign).toBeUndefined();
    expect(fields.pageViews).toBe(0);
  });
});

// ============================================================
// PART 8 — First-Touch Attribution
// ============================================================
describe("PART 8: First-Touch Attribution", () => {
  it("8a. first UTM is preserved when new UTMs arrive", () => {
    setSearchParams("?utm_source=facebook&utm_campaign=first&utm_medium=social");
    captureUtm();

    setSearchParams("?utm_source=twitter&utm_campaign=second&utm_medium=social");
    captureUtm();

    const fields = getTrackingFields();
    expect(fields.source).toBe("facebook");
    expect(fields.campaign).toBe("first");
  });

  it("8b. landingPage preserved from first visit", () => {
    setSearchParams("/landing?utm_source=google&utm_campaign=first&utm_medium=cpc");
    captureUtm();

    setSearchParams("/other-page?utm_source=bing&utm_campaign=second&utm_medium=cpc");
    captureUtm();

    const fields = getTrackingFields();
    expect(fields.landingPage).toContain("/landing");
  });
});

// ============================================================
// PART 9 — Edge Cases
// ============================================================
describe("PART 9: Edge Cases", () => {
  it("9a. missing utm_source with initTracker -> source=direct", () => {
    setSearchParams("?utm_campaign=nosource&utm_medium=email");
    initTracker();

    expect(getTrackingFields().source).toBe("direct");
  });

  it("9b. empty localStorage on load", () => {
    expect(localStorage.getItem(TRACKER_KEY)).toBeNull();
    const fields = getTrackingFields();
    expect(fields.source).toBe("direct");
  });

  it("9c. corrupted JSON handled gracefully", () => {
    localStorage.setItem(TRACKER_KEY, "this is not valid json{{{}");
    const fields = getTrackingFields();
    expect(fields.source).toBe("direct");
  });

  it("9d. corrupted JSON cleared on new UTM", () => {
    localStorage.setItem(TRACKER_KEY, "{broken");
    setSearchParams("?utm_source=google&utm_campaign=recovery&utm_medium=cpc");
    captureUtm();

    const raw = localStorage.getItem(TRACKER_KEY);
    expect(raw).not.toBeNull();
    const data = JSON.parse(raw!);
    expect(data.source).toBe("google");
  });

  it("9e. long session within TTL: data still available", () => {
    initTracker();
    advanceMs(6 * 24 * 60 * 60 * 1000);

    const fields = getTrackingFields();
    expect(fields.source).toBe("direct");
  });

  it("9f. rapid navigation: pageViews increment correctly", () => {
    initTracker();

    for (let i = 0; i < 50; i++) {
      setSearchParams(`/page${i}`);
      trackPageView();
    }

    expect(getTrackingFields().pageViews).toBe(50);
  });

  it("9g. sessionStartAt exists in stored data", () => {
    initTracker();
    const raw = localStorage.getItem(TRACKER_KEY);
    const data = JSON.parse(raw!);
    expect(data.sessionStartAt).toBeDefined();
    expect(() => new Date(data.sessionStartAt)).not.toThrow();
  });
});

// ============================================================
// PART 10 — Data Integrity
// ============================================================
describe("PART 10: Data Integrity", () => {
  it("10a. tracker fields are immutable snapshots", () => {
    initTracker();

    setSearchParams("/properties/1");
    trackPageView();

    const fields1 = getTrackingFields();
    expect(fields1.pageViews).toBe(1);

    setSearchParams("/properties/2");
    trackPageView();

    const fields2 = getTrackingFields();
    expect(fields2.pageViews).toBe(2);
    expect(fields1.pageViews).toBe(1);
  });

  it("10b. referrer is captured at tracker init time", () => {
    setReferrer("https://google.com/search");
    initTracker();

    expect(getTrackingFields().referrer).toBe("https://google.com/search");
  });
});

afterEach(() => {
  vi.useRealTimers();
});
