import { describe, it, expect, beforeEach, vi } from "vitest";
import { getTrackingFields, trackPageView, initTracker } from "../lib/tracker";

function setSearchParams(queryString: string) {
  window.location = new URL(`http://localhost:3000${queryString}`) as unknown as Location;
}

beforeEach(() => {
  localStorage.clear();
  setSearchParams("");
  document.title = "Semsar";
  vi.useFakeTimers();
  vi.setSystemTime(new Date("2025-06-15T12:00:00Z"));
});

// ============================================================
// Form Payload Integration
// ============================================================
describe("Contact Form — Tracking Payload", () => {
  it("includes source, medium, campaign in contact payload", () => {
    setSearchParams("?utm_source=tiktok&utm_campaign=test1&utm_medium=cpc");
    initTracker();

    const fields = getTrackingFields();

    expect(fields.source).toBe("tiktok");
    expect(fields.medium).toBe("cpc");
    expect(fields.campaign).toBe("test1");
  });

  it("includes pageViews and sessionDuration > 0 after browsing", () => {
    initTracker();
    vi.advanceTimersByTime(30_000);

    setSearchParams("/properties/1");
    trackPageView();
    setSearchParams("/contact");
    trackPageView();

    const fields = getTrackingFields();
    expect(fields.pageViews).toBe(2);
    expect(fields.sessionDuration).toBeGreaterThanOrEqual(30);
  });

  it("includes landingPage, currentPage, visitHistory, userAgent", () => {
    setSearchParams("/properties/123?utm_source=fb&utm_campaign=ads&utm_medium=social");
    initTracker();
    setSearchParams("/contact");
    trackPageView();

    const fields = getTrackingFields();
    expect(fields.landingPage).toBe("/properties/123?utm_source=fb&utm_campaign=ads&utm_medium=social");
    expect(fields.currentPage).toBe("/contact");
    expect(fields.visitHistory).toBeDefined();
    expect(fields.userAgent).toBeDefined();
  });

  it("fallback: source=direct when no UTMs", () => {
    initTracker();
    const fields = getTrackingFields();
    expect(fields.source).toBe("direct");
    expect(fields.medium).toBeUndefined();
    expect(fields.campaign).toBeUndefined();
  });
});

describe("Booking Form — Tracking Payload", () => {
  it("includes all tracking fields from getTrackingFields", () => {
    setSearchParams("?utm_source=instagram&utm_campaign=summer&utm_medium=social");
    initTracker();
    vi.advanceTimersByTime(45_000);
    setSearchParams("/properties/1");
    trackPageView();
    setSearchParams("/booking");
    trackPageView();

    const fields = getTrackingFields();
    const payload = {
      propertyId: null,
      unitId: 123,
      name: "Test User",
      phone: "+201000000000",
      ...fields,
    };

    expect(payload.source).toBe("instagram");
    expect(payload.medium).toBe("social");
    expect(payload.campaign).toBe("summer");
    expect(payload.landingPage).toContain("utm_source=instagram");
    expect(payload.currentPage).toBe("/booking");
    expect(payload.pageViews).toBe(2);
    expect(payload.sessionDuration).toBeGreaterThanOrEqual(45);
    expect(payload.visitHistory).toBeDefined();
  });
});

describe("Land Request Form — Tracking Payload", () => {
  it("includes all tracking fields from getTrackingFields", () => {
    setSearchParams("?utm_source=email&utm_campaign=land&utm_medium=email");
    initTracker();
    vi.advanceTimersByTime(60_000);
    setSearchParams("/land-request");
    trackPageView();

    const fields = getTrackingFields();
    const payload = {
      name: "Test User",
      phone: "+201000000000",
      location: "Hurghada",
      ...fields,
    };

    expect(payload.source).toBe("email");
    expect(payload.medium).toBe("email");
    expect(payload.campaign).toBe("land");
    expect(payload.currentPage).toBe("/land-request");
    expect(payload.pageViews).toBe(1);
    expect(payload.sessionDuration).toBeGreaterThanOrEqual(60);
    expect(payload.visitHistory).toBeDefined();
  });
});

// ============================================================
// Spread Integrity
// ============================================================
describe("Spread Integrity — getTrackingFields() with form data", () => {
  it("form-specific fields are NOT overwritten by tracking fields", () => {
    initTracker();

    const trackingFields = getTrackingFields();

    const contactPayload = {
      name: "John",
      phone: "+201234567890",
      message: "Hello",
      ...trackingFields,
    };

    expect(contactPayload.name).toBe("John");
    expect(contactPayload.phone).toBe("+201234567890");
    expect(contactPayload.message).toBe("Hello");
    expect(contactPayload.source).toBe("direct");
  });

  it("tracking fields do not contain null for required fields", () => {
    initTracker();

    const fields = getTrackingFields();
    expect(fields.source).toBeDefined();
    expect(fields.pageViews).toBeDefined();
    expect(fields.source).not.toBeNull();
  });
});

// ============================================================
// Edge Cases in Form Submission
// ============================================================
describe("Edge Cases in Form Submission", () => {
  it("multiple visits: first-touch attribution preserved", () => {
    setSearchParams("/first?utm_source=facebook&utm_campaign=original&utm_medium=social");
    initTracker();
    vi.advanceTimersByTime(10_000);
    setSearchParams("/second");
    trackPageView();
    vi.advanceTimersByTime(10_000);
    setSearchParams("/third");
    trackPageView();

    const fields = getTrackingFields();
    expect(fields.source).toBe("facebook");
    expect(fields.campaign).toBe("original");
    expect(fields.pageViews).toBe(2);
    expect(fields.landingPage).toContain("/first");
  });

  it("initTracker for direct visit gets source=direct with page views", () => {
    initTracker();
    setSearchParams("/properties/1");
    trackPageView();
    setSearchParams("/contact");
    trackPageView();

    const fields = getTrackingFields();
    expect(fields.source).toBe("direct");
    expect(fields.pageViews).toBe(2);
    expect(fields.landingPage).toBe("/");
    expect(fields.currentPage).toBe("/contact");
    expect(fields.visitHistory).toBeDefined();
    expect(fields.sessionDuration).toBeGreaterThanOrEqual(0);
  });

  it("sessionDuration for direct visitor is accurate", () => {
    initTracker();
    vi.advanceTimersByTime(90_000);
    setSearchParams("/contact");
    trackPageView();

    const fields = getTrackingFields();
    expect(fields.source).toBe("direct");
    expect(fields.pageViews).toBe(1);
    expect(fields.sessionDuration).toBeGreaterThanOrEqual(85);
  });
});

afterEach(() => {
  vi.useRealTimers();
});
