const CRAWLERS = /bot|crawler|spider|facebookexternalhit|twitterbot|linkedinbot|embedly|slack|whatsapp|telegram|pinterest|discord/i;

const API_BASE = 'https://semsar-hub.runasp.net/api';

async function fetchJSON(url: string) {
  try {
    const res = await fetch(url, { headers: { 'User-Agent': 'Semsar-Middleware' } });
    if (!res.ok) return null;
    return res.json();
  } catch { return null; }
}

function buildHtml(title: string, description: string, image: string, url: string, lang = 'en') {
  const dir = lang === 'ar' ? 'rtl' : 'ltr';
  return `<!doctype html>
<html lang="${lang}" dir="${dir}">
<head>
<meta charset="UTF-8" />
<meta name="viewport" content="width=device-width, initial-scale=1.0" />
<title>${title}</title>
<meta name="description" content="${description}" />
<meta property="og:title" content="${title}" />
<meta property="og:description" content="${description}" />
<meta property="og:image" content="${image}" />
<meta property="og:url" content="${url}" />
<meta property="og:type" content="website" />
<meta property="og:locale" content="${lang === 'ar' ? 'ar_EG' : 'en_US'}" />
<meta name="twitter:card" content="summary_large_image" />
<meta name="twitter:title" content="${title}" />
<meta name="twitter:description" content="${description}" />
<meta name="twitter:image" content="${image}" />
<link rel="canonical" href="${url}" />
</head>
<body><script>window.location.href="${url}";</script></body>
</html>`;
}

export const config = {
  runtime: 'edge',
};

export default async function middleware(request: Request) {
  const ua = request.headers.get('user-agent') || '';
  if (!CRAWLERS.test(ua)) return;

  const url = new URL(request.url);
  const path = url.pathname;
  const lang = path.startsWith('/ar') ? 'ar' : 'en';
  const cleanPath = path.replace(/^\/(en|ar)\/?/, '/').replace(/\/$/, '') || '/';

  const origin = `https://semsar-web-alpha.vercel.app`;

  if (cleanPath.startsWith('/properties/')) {
    const slug = cleanPath.replace('/properties/', '');
    const data = await fetchJSON(`${API_BASE}/Properties/slug/${slug}`);
    if (data) {
      const title = lang === 'ar' ? data.titleAr : data.titleEn;
      const desc = (lang === 'ar' ? data.descriptionAr : data.descriptionEn)?.slice(0, 160) || title;
      const image = data.images?.[0] || data.image || `${origin}/og-image.svg`;
      return new Response(buildHtml(title, desc, image, `${origin}${path}`, lang), {
        headers: { 'content-type': 'text/html;charset=utf-8' },
      });
    }
  }

  if (cleanPath.startsWith('/projects/')) {
    const slug = cleanPath.replace('/projects/', '');
    const data = await fetchJSON(`${API_BASE}/Projects/slug/${slug}`);
    if (data) {
      const title = lang === 'ar' ? data.titleAr : data.titleEn;
      const desc = (lang === 'ar' ? data.descriptionAr : data.descriptionEn)?.slice(0, 160) || title;
      const image = data.images?.[0] || data.image || `${origin}/og-image.svg`;
      return new Response(buildHtml(title, desc, image, `${origin}${path}`, lang), {
        headers: { 'content-type': 'text/html;charset=utf-8' },
      });
    }
  }

  if (cleanPath.startsWith('/units/')) {
    const slug = cleanPath.replace('/units/', '');
    const data = await fetchJSON(`${API_BASE}/Properties/slug/${slug}`);
    if (data) {
      const title = lang === 'ar' ? data.titleAr : data.titleEn;
      const desc = (lang === 'ar' ? data.descriptionAr : data.descriptionEn)?.slice(0, 160) || title;
      const image = data.images?.[0] || data.image || `${origin}/og-image.svg`;
      return new Response(buildHtml(title, desc, image, `${origin}${path}`, lang), {
        headers: { 'content-type': 'text/html;charset=utf-8' },
      });
    }
  }
}
