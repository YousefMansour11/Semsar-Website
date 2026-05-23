export function localizedPath(path: string, lang: string): string {
  if (!path || typeof path !== 'string') return path
  if (path.startsWith('http') || path.startsWith('#') || path.startsWith('tel:') || path.startsWith('mailto:')) return path
  const stripped = path.replace(/^\/(en|ar)(\/|$)/, '/')
  if (stripped === '/' || stripped === '') return `/${lang}`
  return `/${lang}${stripped}`
}

export function stripLanguagePrefix(path: string): string {
  const m = path.match(/^\/(en|ar)(\/.*)?$/)
  return m ? (m[2] || '/') : path
}

export function getSiteUrl(): string {
  if (typeof window !== 'undefined') return window.location.origin
  return 'https://semsar-alpha.vercel.app'
}

export function buildAlternates(
  path: string,
  origin: string,
  includeXDefault = false
): { hrefLang: string; href: string }[] {
  const alts = [
    { hrefLang: 'en', href: `${origin}${localizedPath(path, 'en')}` },
    { hrefLang: 'ar', href: `${origin}${localizedPath(path, 'ar')}` },
  ]
  if (includeXDefault) {
    alts.push({ hrefLang: 'x-default', href: `${origin}${localizedPath(path, 'en')}` })
  }
  return alts
}
