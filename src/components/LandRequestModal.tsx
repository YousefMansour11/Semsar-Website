import React, { useState, useCallback, useRef } from 'react';
import { X, CheckCircle2, MapPin } from 'lucide-react';
import { useLanguage } from '../i18n/LanguageContext';
import type { LandRequestPayload } from '../types/property';
import { submitLandRequest, ApiError } from '../lib/api';
import { getTrackingFields } from '../lib/tracker';

interface LandRequestModalProps {
  isOpen: boolean;
  onClose: () => void;
}

export function LandRequestModal({ isOpen, onClose }: LandRequestModalProps) {
  const { t, language } = useLanguage();
  const [status, setStatus] = useState<'idle' | 'submitting' | 'success'>('idle');
  const [form, setForm] = useState({
    name: '', phone: '', governorate: '', city: '', area: '',
    minPrice: '', maxPrice: '', minArea: '', maxArea: '', notes: '',
  });

  const [errorMsg, setErrorMsg] = useState<string | null>(null);
  const submittingRef = useRef(false);
  const formStartTime = useRef(Date.now());

  const handleClose = useCallback(() => { setStatus('idle'); setForm(prev => ({ ...prev, name: '', phone: '', governorate: '', city: '', area: '', minPrice: '', maxPrice: '', minArea: '', maxArea: '', notes: '' })); setErrorMsg(null); onClose(); }, [onClose]);

  React.useEffect(() => {
    if (!isOpen) return;
    formStartTime.current = Date.now();
    const handler = (e: KeyboardEvent) => { if (e.key === 'Escape') handleClose(); };
    window.addEventListener('keydown', handler);
    return () => window.removeEventListener('keydown', handler);
  }, [isOpen, handleClose]);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (submittingRef.current) return;
    if (Date.now() - formStartTime.current < 3000) {
      setErrorMsg(t('error.submitFailed'));
      return;
    }
    submittingRef.current = true;
    setStatus('submitting');
    setErrorMsg(null);

    const locationStr = [form.governorate, form.city, form.area].filter(Boolean).join(', ');
    const payload: LandRequestPayload = {
      name: form.name,
      phone: form.phone,
      location: locationStr,
      minPrice: form.minPrice ? Number(form.minPrice) : undefined,
      maxPrice: form.maxPrice ? Number(form.maxPrice) : undefined,
      minArea: form.minArea ? Number(form.minArea) : undefined,
      maxArea: form.maxArea ? Number(form.maxArea) : undefined,
      notes: form.notes || undefined,
      ...getTrackingFields(),
    };

    const phoneDigits = form.phone.replace(/[^\d+]/g, '');
    if (phoneDigits.length < 8) {
      setErrorMsg(t('form.error.invalidPhone'));
      setStatus('idle');
      return;
    }
    const minP = form.minPrice ? Number(form.minPrice) : 0;
    const maxP = form.maxPrice ? Number(form.maxPrice) : 0;
    if (minP > 0 && maxP > 0 && minP > maxP) {
      setErrorMsg(t('form.error.minPriceExceedsMax'));
      setStatus('idle');
      return;
    }
    const minA = form.minArea ? Number(form.minArea) : 0;
    const maxA = form.maxArea ? Number(form.maxArea) : 0;
    if (minA > 0 && maxA > 0 && minA > maxA) {
      setErrorMsg(t('form.error.minAreaExceedsMax'));
      setStatus('idle');
      return;
    }
    try {
      await submitLandRequest({ ...payload, phone: phoneDigits });
      setStatus('success');
    } catch (err) {
      const msg = err instanceof ApiError ? err.message : t('error.submitFailed');
      setErrorMsg(msg);
      setStatus('idle');
    } finally {
      submittingRef.current = false;
    }
  };

  if (!isOpen) return null;

  const inputCls = "w-full px-3 py-2 rounded-xl bg-background border border-border focus:border-secondary focus:ring-2 focus:ring-secondary/20 transition-all";
  const fmtDisp = (v: string) => v ? Number(v.replace(/,/g, '')).toLocaleString('en-US') : '';

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center p-4 sm:p-6 overflow-y-auto" role="dialog" aria-modal="true" aria-label={t('land.modalTitle')}>
      <div className="absolute inset-0 bg-navy/80 backdrop-blur-sm" onClick={handleClose} aria-hidden="true" />

      <div className="relative w-full max-w-lg bg-white rounded-2xl shadow-2xl overflow-hidden animate-in fade-in zoom-in-95 duration-200 my-0 sm:my-6 max-h-[90vh] sm:max-h-[85vh] flex flex-col">
        <button onClick={handleClose} aria-label={t('modal.close')} className={`absolute top-3 ${language === 'ar' ? 'left-3' : 'right-3'} text-muted-foreground hover:text-foreground transition-colors p-1.5 rounded-full hover:bg-muted z-10`} autoFocus>
          <X className="w-4 h-4" />
        </button>

        <div className="bg-gradient-to-r from-gold to-gold-dark px-4 sm:px-6 py-3 sm:py-5 shrink-0">
          <div className="flex items-center gap-2 mb-1">
            <MapPin className="w-5 h-5 text-navy" />
            <h2 className="text-lg sm:text-xl font-display font-bold text-navy">{t('land.modalTitle')}</h2>
          </div>
          <p className="text-navy/70 text-xs sm:text-sm">{t('land.subtitle')}</p>
        </div>

        <div className="flex flex-col flex-1 overflow-hidden" aria-live="polite" aria-atomic="true">
          {status === 'success' ? (
            <div className="overflow-y-auto flex-1 p-4 sm:p-6 text-center">
              <div className="py-6">
                <div className="w-14 h-14 bg-green-100 text-green-600 rounded-full flex items-center justify-center mx-auto mb-4">
                  <CheckCircle2 className="w-7 h-7" />
                </div>
                <h3 className="text-xl font-display font-bold mb-1">{t('land.form.success')}</h3>
                <p className="text-sm text-muted-foreground">{t('land.form.successDesc')}</p>
                <button onClick={handleClose} className="mt-6 w-full min-h-[40px] py-2.5 bg-navy text-white rounded-xl font-medium hover:bg-navy-light transition-colors active:scale-[0.98] text-sm">
                  {t('modal.cancel')}
                </button>
              </div>
            </div>
          ) : (
            <form onSubmit={handleSubmit} autoComplete="off" className="flex flex-col flex-1 overflow-hidden">
              <input type="text" name="website" tabIndex={-1} autoComplete="off" className="absolute opacity-0 pointer-events-none h-0 w-0" aria-hidden="true" />
              <div className="overflow-y-auto flex-1 p-4 sm:p-6 space-y-2">
                <div className="grid grid-cols-1 sm:grid-cols-2 gap-2">
                  <div>
                    <label htmlFor="lr-name" className="block text-xs font-medium mb-1">{t('land.form.name')}</label>
                    <input id="lr-name" type="text" autoComplete="name" required value={form.name} onChange={e => setForm({ ...form, name: e.target.value })} className={inputCls} />
                  </div>
                  <div>
                    <label htmlFor="lr-phone" className="block text-xs font-medium mb-1">{t('land.form.phone')}</label>
                    <input id="lr-phone" type="tel" autoComplete="tel" required dir="ltr" value={form.phone} onChange={e => setForm({ ...form, phone: e.target.value })} className={inputCls} placeholder="+20 100 000 0000" />
                  </div>
                </div>

                <div className="grid grid-cols-1 sm:grid-cols-3 gap-2">
                  <div>
                    <label htmlFor="lr-governorate" className="block text-xs font-medium mb-1">{t('land.form.governorate')}</label>
                    <input
                      id="lr-governorate"
                      type="text"
                      autoComplete="off"
                      required
                      value={form.governorate}
                      onChange={e => setForm({ ...form, governorate: e.target.value })}
                      className={inputCls}
                    />
                  </div>
                  <div>
                    <label htmlFor="lr-city" className="block text-xs font-medium mb-1">{t('land.form.city')}</label>
                    <input
                      id="lr-city"
                      type="text"
                      autoComplete="off"
                      required
                      value={form.city}
                      onChange={e => setForm({ ...form, city: e.target.value })}
                      className={inputCls}
                    />
                  </div>
                  <div>
                    <label htmlFor="lr-area" className="block text-xs font-medium mb-1">{t('land.form.area')}</label>
                    <input
                      id="lr-area"
                      type="text"
                      autoComplete="off"
                      value={form.area}
                      onChange={e => setForm({ ...form, area: e.target.value })}
                      className={inputCls}
                    />
                  </div>
                </div>

                <div className="grid grid-cols-1 sm:grid-cols-2 gap-2">
                  <div>
                    <label htmlFor="lr-minPrice" className="block text-xs font-medium mb-1">{t('filters.minPrice')} ({t('general.currency')})</label>
                    <input id="lr-minPrice" type="text" inputMode="numeric" autoComplete="off" value={fmtDisp(form.minPrice)} onChange={e => setForm({ ...form, minPrice: e.target.value.replace(/,/g, '') })} className={inputCls} placeholder={t('filters.placeholder.min')} />
                </div>
                <div>
                  <label htmlFor="lr-maxPrice" className="block text-xs font-medium mb-1">{t('filters.maxPrice')} ({t('general.currency')})</label>
                  <input id="lr-maxPrice" type="text" inputMode="numeric" autoComplete="off" value={fmtDisp(form.maxPrice)} onChange={e => setForm({ ...form, maxPrice: e.target.value.replace(/,/g, '') })} className={inputCls} placeholder={t('general.placeholder.any')} />
                  </div>
                </div>

                <div className="grid grid-cols-1 sm:grid-cols-2 gap-2">
                  <div>
                    <label htmlFor="lr-minArea" className="block text-xs font-medium mb-1">{t('land.form.minArea')} ({t('general.m2')})</label>
                    <input id="lr-minArea" type="text" inputMode="numeric" autoComplete="off" value={fmtDisp(form.minArea)} onChange={e => setForm({ ...form, minArea: e.target.value.replace(/,/g, '') })} className={inputCls} placeholder={t('filters.placeholder.min')} />
                </div>
                <div>
                  <label htmlFor="lr-maxArea" className="block text-xs font-medium mb-1">{t('land.form.maxArea')} ({t('general.m2')})</label>
                  <input id="lr-maxArea" type="text" inputMode="numeric" autoComplete="off" value={fmtDisp(form.maxArea)} onChange={e => setForm({ ...form, maxArea: e.target.value.replace(/,/g, '') })} className={inputCls} placeholder={t('general.placeholder.any')} />
                  </div>
                </div>

                <div>
                  <label htmlFor="lr-notes" className="block text-xs font-medium mb-1">{t('land.form.notes')}</label>
                  <textarea id="lr-notes" rows={2} value={form.notes} onChange={e => setForm({ ...form, notes: e.target.value })} className={`${inputCls} resize-none`} />
                </div>

                {errorMsg && (
                  <p className="text-sm text-rose-600 bg-rose-50 border border-rose-200 rounded-lg px-3 py-2" role="alert">{errorMsg}</p>
                )}
              </div>

              <div className="flex flex-col-reverse sm:flex-row gap-2 shrink-0 px-4 sm:px-6 pb-4 sm:pb-6">
                <button type="button" onClick={handleClose} className="w-full sm:flex-1 min-h-[40px] py-2.5 rounded-xl border border-border font-medium hover:bg-muted transition-colors active:scale-[0.98] text-sm">
                  {t('modal.cancel')}
                </button>
                <button type="submit" disabled={status === 'submitting'} className="w-full sm:flex-[2] min-h-[40px] py-2.5 bg-gold text-navy rounded-xl font-bold shadow-lg shadow-amber-900/20 hover:bg-gold-dark hover:text-white hover:shadow-xl transition-all disabled:opacity-70 active:scale-[0.98] text-sm">
                  {status === 'submitting' ? t('general.loading') : t('land.form.submit')}
                </button>
              </div>
            </form>
          )}
        </div>
      </div>
    </div>
  );
}
