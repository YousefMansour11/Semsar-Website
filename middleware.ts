const CRAWLERS = /bot|crawler|spider|facebookexternalhit|twitterbot|linkedinbot|embedly|slack|whatsapp|telegram|pinterest|discord/i;

const API_BASE = 'https://semsar-hub.runasp.net/api';
const ORIGIN = 'https://semsar-web-alpha.vercel.app';

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
<body><meta http-equiv="refresh" content="0;url=${url}" /></body>
</html>`;
}

export const config = {
  runtime: 'edge',
};

const NOT_FOUND_HTML = (path: string, lang: string) => buildHtml(
  lang === 'ar' ? 'الصفحة غير موجودة - سمسار' : 'Page Not Found - Semsar',
  lang === 'ar' ? 'الصفحة التي تبحث عنها غير موجودة' : 'The page you are looking for does not exist.',
  `${ORIGIN}/og-image.png`,
  `${ORIGIN}${path}`,
  lang
);

export default async function middleware(request: Request): Promise<Response | void> {
  const ua = request.headers.get('user-agent') || '';
  if (!CRAWLERS.test(ua)) return;

  const url = new URL(request.url);
  const path = url.pathname;
  const lang = path.startsWith('/ar') ? 'ar' : 'en';
  const cleanPath = path.replace(/^\/(en|ar)\/?/, '/').replace(/\/$/, '') || '/';

  const notFound = () => new Response(NOT_FOUND_HTML(path, lang), {
    status: 404,
    headers: { 'content-type': 'text/html;charset=utf-8' },
  });

  const render = (title: string, description: string, image: string) =>
    new Response(buildHtml(title, description, image, `${ORIGIN}${path}`, lang), {
      headers: { 'content-type': 'text/html;charset=utf-8' },
    });

  if (cleanPath.startsWith('/properties/')) {
    const slug = cleanPath.replace('/properties/', '');
    const data = await fetchJSON(`${API_BASE}/Properties/slug/${slug}`);
    if (!data) return notFound();
    return render(
      lang === 'ar' ? data.titleAr : data.titleEn,
      (lang === 'ar' ? data.descriptionAr : data.descriptionEn)?.slice(0, 160) || data.titleEn,
      data.images?.[0] || data.image || `${ORIGIN}/og-image.png`
    );
  }

  if (cleanPath.startsWith('/projects/')) {
    const slug = cleanPath.replace('/projects/', '');
    const data = await fetchJSON(`${API_BASE}/Projects/slug/${slug}`);
    if (!data) return notFound();
    return render(
      lang === 'ar' ? data.nameAr : data.nameEn,
      (lang === 'ar' ? data.descriptionAr : data.descriptionEn)?.slice(0, 160) || data.nameEn,
      data.images?.[0] || data.image || `${ORIGIN}/og-image.png`
    );
  }

  if (cleanPath.startsWith('/units/')) {
    const slug = cleanPath.replace('/units/', '');
    const data = await fetchJSON(`${API_BASE}/Properties/slug/${slug}`);
    if (!data) return notFound();
    return render(
      lang === 'ar' ? data.titleAr : data.titleEn,
      (lang === 'ar' ? data.descriptionAr : data.descriptionEn)?.slice(0, 160) || data.titleEn,
      data.images?.[0] || data.image || `${ORIGIN}/og-image.png`
    );
  }
}
