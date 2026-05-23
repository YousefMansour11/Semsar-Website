const TRACKER_KEY = "semsar_tracker";
const TTL = 7 * 24 * 60 * 60 * 1000;
const SESSION_IDLE_THRESHOLD = 30 * 60 * 1000;
const MAX_VISIT_HISTORY = 15;

export interface PageVisit {
  path: string;
  title: string;
  timestamp: string;
  referrer: string;
}

export interface TrackerData {
  source: string;
  medium?: string;
  campaign?: string;
  content?: string;
  term?: string;
  landingPage: string;
  firstVisitAt: string;
  sessionStartAt: string;
  referrer: string;
  userAgent: string;
  pageViews: number;
  lastReferrer?: string;
  visitHistory: PageVisit[];
  _expiresAt: number;
}

function isExpired(data: TrackerData): boolean {
  return Date.now() > data._expiresAt;
}

function loadTracker(): TrackerData | null {
  try {
    const raw = localStorage.getItem(TRACKER_KEY);
    if (!raw) return null;
    const data: TrackerData = JSON.parse(raw);
    if (isExpired(data)) {
      localStorage.removeItem(TRACKER_KEY);
      return null;
    }
    return data;
  } catch {
    return null;
  }
}

function saveTracker(data: TrackerData): void {
  try {
    localStorage.setItem(TRACKER_KEY, JSON.stringify(data));
  } catch { /* localStorage may be full or disabled */ }
}

function initTrackerData(source: string, params?: { source?: string | null; medium?: string | null; campaign?: string | null; content?: string | null; term?: string | null }): TrackerData {
  const now = new Date().toISOString();
  return {
    source,
    medium: params?.medium?.trim().toLowerCase() || undefined,
    campaign: params?.campaign?.trim().toLowerCase() || undefined,
    content: params?.content?.trim().toLowerCase() || undefined,
    term: params?.term?.trim().toLowerCase() || undefined,
    landingPage: window.location.pathname + window.location.search,
    firstVisitAt: now,
    sessionStartAt: now,
    referrer: document.referrer || "",
    userAgent: navigator.userAgent,
    pageViews: 0,
    visitHistory: [],
    _expiresAt: Date.now() + TTL,
  };
}

export function initTracker(): void {
  if (typeof window === "undefined") return;
  const stored = loadTracker();
  if (stored) return;

  const params = new URLSearchParams(window.location.search);
  const source = params.get("utm_source");
  if (source) {
    const tracker = initTrackerData(source.trim().toLowerCase(), {
      source,
      medium: params.get("utm_medium"),
      campaign: params.get("utm_campaign"),
      content: params.get("utm_content"),
      term: params.get("utm_term"),
    });
    saveTracker(tracker);
  } else {
    const tracker = initTrackerData("direct");
    saveTracker(tracker);
  }
}

export function captureUtm(): void {
  if (typeof window === "undefined") return;

  const stored = loadTracker();
  if (stored) return;

  const params = new URLSearchParams(window.location.search);
  const source = params.get("utm_source");
  if (!source) return;

  const tracker = initTrackerData(source.trim().toLowerCase(), {
    source,
    medium: params.get("utm_medium"),
    campaign: params.get("utm_campaign"),
    content: params.get("utm_content"),
    term: params.get("utm_term"),
  });
  saveTracker(tracker);
}

function isNewSession(stored: TrackerData): boolean {
  if (stored.visitHistory.length === 0) return false;
  const lastVisit = new Date(stored.visitHistory[stored.visitHistory.length - 1].timestamp).getTime();
  return Date.now() - lastVisit > SESSION_IDLE_THRESHOLD;
}

export function trackPageView(): void {
  if (typeof window === "undefined") return;

  const stored = loadTracker();
  if (!stored) return;

  if (isNewSession(stored)) {
    stored.sessionStartAt = new Date().toISOString();
  }

  const now = new Date().toISOString();
  const visit: PageVisit = {
    path: window.location.pathname + window.location.search,
    title: document.title,
    timestamp: now,
    referrer: document.referrer || stored.referrer || "",
  };

  stored.pageViews += 1;
  stored.lastReferrer = document.referrer || stored.lastReferrer;
  stored.visitHistory.push(visit);
  if (stored.visitHistory.length > MAX_VISIT_HISTORY) {
    stored.visitHistory = stored.visitHistory.slice(-MAX_VISIT_HISTORY);
  }

  saveTracker(stored);
}

export function getTrackingFields(): {
  source: string;
  medium?: string;
  campaign?: string;
  content?: string;
  term?: string;
  landingPage?: string;
  firstVisitAt?: string;
  currentPage?: string;
  referrer?: string;
  userAgent?: string;
  pageViews: number;
  sessionDuration?: number;
  lastReferrer?: string;
  visitHistory?: string;
} {
  const stored = loadTracker();
  if (!stored) return { source: "direct", pageViews: 0 };

  const now = Date.now();
  const sessionStartMs = new Date(stored.sessionStartAt).getTime();
  const sessionDuration = isNaN(sessionStartMs) ? undefined : Math.floor((now - sessionStartMs) / 1000);

  return {
    source: stored.source,
    medium: stored.medium,
    campaign: stored.campaign,
    content: stored.content,
    term: stored.term,
    landingPage: stored.landingPage,
    firstVisitAt: stored.firstVisitAt,
    currentPage: typeof window !== "undefined" ? window.location.pathname : undefined,
    referrer: stored.referrer || undefined,
    userAgent: stored.userAgent || undefined,
    pageViews: stored.pageViews,
    sessionDuration: sessionDuration,
    lastReferrer: stored.lastReferrer || undefined,
    visitHistory: stored.visitHistory.length > 0 ? JSON.stringify(stored.visitHistory) : undefined,
  };
}
