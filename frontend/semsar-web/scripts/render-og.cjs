const puppeteer = require('puppeteer');
const fs = require('fs');
const path = require('path');

(async () => {
  const svgPath = path.resolve(__dirname, '../public/og-image.svg');
  const svg = fs.readFileSync(svgPath, 'utf-8');
  const html = `<!DOCTYPE html><html><body style="margin:0">${svg}</body></html>`;
  const browser = await puppeteer.launch({ headless: true });
  const page = await browser.newPage();
  await page.setViewport({ width: 1200, height: 630 });
  await page.setContent(html, { waitUntil: 'networkidle0' });
  await page.screenshot({ path: path.resolve(__dirname, '../public/og-image.png'), fullPage: false, type: 'png' });
  await browser.close();
  const size = fs.statSync(path.resolve(__dirname, '../public/og-image.png')).size;
  console.log('PNG created:', size, 'bytes');
})();
