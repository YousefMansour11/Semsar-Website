const CACHE_KEY = 'semsar_img_cache';

interface Cache {
  [url: string]: boolean;
}

function getCache(): Cache {
  try {
    const raw = sessionStorage.getItem(CACHE_KEY);
    return raw ? JSON.parse(raw) : {};
  } catch {
    return {};
  }
}

function setCache(cache: Cache): void {
  try {
    sessionStorage.setItem(CACHE_KEY, JSON.stringify(cache));
  } catch {
    // sessionStorage full or unavailable
  }
}

async function headExists(url: string): Promise<boolean> {
  try {
    const res = await fetch(url, { method: 'HEAD', signal: AbortSignal.timeout(5000) });
    return res.ok;
  } catch {
    return true;
  }
}

export async function validateImageUrls(urls: string[]): Promise<string[]> {
  const cache = getCache();
  const results: string[] = [];
  const todo: string[] = [];

  for (const url of urls) {
    if (!url) continue;
    if (cache[url] === true) {
      results.push(url);
    } else if (cache[url] === false) {
      continue;
    } else if (!url.includes('res.cloudinary.com')) {
      cache[url] = true;
      results.push(url);
    } else {
      todo.push(url);
    }
  }

  if (todo.length > 0) {
    const checks = await Promise.all(
      todo.map(async (url) => {
        const exists = await headExists(url);
        cache[url] = exists;
        return exists ? url : null;
      })
    );
    setCache(cache);
    for (const url of checks) {
      if (url) results.push(url);
    }
  }

  return results;
}
