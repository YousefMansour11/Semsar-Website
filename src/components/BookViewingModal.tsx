import React, { useState, useCallback, useRef } from 'react';
import { X, CheckCircle2 } from 'lucide-react';
import { useLanguage } from '../i18n/LanguageContext';
import type { BookingPayload } from '../types/property';
import { submitBooking, ApiError } from '../lib/api';
import { getTrackingFields } from '../lib/tracker';

interface BookViewingModalProps {
  isOpen: boolean;
  onClose: () => void;
  // Backend booking distinguishes property vs unit. Caller passes one of these.
  propertyId?: number | null;
  unitId?: number | null;
}

export function BookViewingModal({ isOpen, onClose, propertyId = null, unitId = null }: BookViewingModalProps) {
  const { t, language } = useLanguage();
  const [status, setStatus] = useState<'idle' | 'submitting' | 'success'>('idle');
  const [form, setForm] = useState({ name: '', phone: '', message: '' });

  const [errorMsg, setErrorMsg] = useState<string | null>(null);
  const submittingRef = useRef(false);

  const formStartTime = useRef(Date.now());

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (submittingRef.current) return;
    if (Date.now() - formStartTime.current < 3000) {
      setErrorMsg(t('form.error.submitFailed'));
      return;
    }
    submittingRef.current = true;
    setStatus('submitting');
    setErrorMsg(null);

    const safeId = (id: number | null | undefined) => (id != null && id > 0 && !isNaN(id) ? id : null);
    const uId = safeId(unitId);
    const pId = safeId(propertyId);
    if (!uId && !pId) {
      setErrorMsg(t('form.error.submitFailed'));
      setStatus('idle');
      submittingRef.current = false;
      return;
    }
    const payload: BookingPayload = {
      propertyId: pId,
      unitId: uId,
      name: form.name,
      phone: form.phone,
      message: form.message || undefined,
      ...getTrackingFields(),
    };

    const phoneDigits = form.phone.replace(/[^\d+]/g, '');
    if (phoneDigits.length < 8) {
      setErrorMsg(t('form.error.invalidPhone'));
      setStatus('idle');
      return;
    }
    try {
      await submitBooking({ ...payload, phone: phoneDigits }, new Date(formStartTime.current).toISOString());
      setStatus('success');
    } catch (err) {
      const msg = err instanceof ApiError ? err.message : t('error.submitFailed');
      setErrorMsg(msg);
      setStatus('idle');
    } finally {
      submittingRef.current = false;
    }
  };

  const handleClose = useCallback(() => { setStatus('idle'); setForm({ name: '', phone: '', message: '' }); setErrorMsg(null); onClose(); }, [onClose]);

  React.useEffect(() => {
    if (!isOpen) return;
    formStartTime.current = Date.now();
    const handler = (e: KeyboardEvent) => { if (e.key === 'Escape') handleClose(); };
    window.addEventListener('keydown', handler);
    return () => window.removeEventListener('keydown', handler);
  }, [isOpen, handleClose]);

  if (!isOpen) return null;

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center p-4 animate-in fade-in duration-200" role="dialog" aria-modal="true" aria-label={t('modal.title')}>
      <div className="absolute inset-0 bg-navy/70 backdrop-blur-md" onClick={handleClose} aria-hidden="true" />
      <div className="relative w-full max-w-md bg-white rounded-2xl shadow-2xl shadow-black/10 overflow-hidden animate-in zoom-in-95 duration-200">
        <button onClick={handleClose} aria-label={t('modal.close')} className={`absolute top-4 ${language === 'ar' ? 'left-4' : 'right-4'} text-muted-foreground hover:text-foreground p-2 rounded-full hover:bg-muted z-10`} autoFocus>
          <X className="w-5 h-5" />
        </button>
        <div className="p-8" aria-live="polite" aria-atomic="true">
          {status === 'success' ? (
            <div className="text-center py-8">
              <div className="w-16 h-16 bg-green-100 text-green-600 rounded-full flex items-center justify-center mx-auto mb-6">
                <CheckCircle2 className="w-8 h-8" />
              </div>
              <h3 className="text-2xl font-display font-bold mb-2">{t('modal.success')}</h3>
              <p className="text-muted-foreground">{t('modal.successDesc')}</p>
              <button onClick={handleClose} className="mt-8 w-full py-3 bg-navy text-white rounded-xl font-medium hover:bg-navy-light transition-colors">
                {t('modal.cancel')}
              </button>
            </div>
          ) : (
            <>
              <h2 className="text-2xl font-display font-bold mb-2">{t('modal.title')}</h2>
              <p className="text-muted-foreground mb-6">{t('modal.subtitle')}</p>
              <form onSubmit={handleSubmit} autoComplete="off" className="space-y-4">
                <input type="text" name="website" tabIndex={-1} autoComplete="off" className="absolute opacity-0 pointer-events-none h-0 w-0" aria-hidden="true" />
                <div>
                  <label htmlFor="bv-name" className="block text-sm font-medium mb-1.5">{t('modal.name')}</label>
                  <input id="bv-name" type="text" autoComplete="name" required value={form.name} onChange={e => setForm({ ...form, name: e.target.value })} className="w-full px-4 py-3 rounded-xl bg-background border border-border focus:border-secondary focus:ring-2 focus:ring-secondary/20 transition-all" />
                </div>
                <div>
                  <label htmlFor="bv-phone" className="block text-sm font-medium mb-1.5">{t('modal.phone')}</label>
                  <input id="bv-phone" type="tel" autoComplete="tel" required dir="ltr" value={form.phone} onChange={e => setForm({ ...form, phone: e.target.value })} className="w-full px-4 py-3 rounded-xl bg-background border border-border focus:border-secondary focus:ring-2 focus:ring-secondary/20 transition-all" placeholder="+20 100 000 0000" />
                </div>
                <div>
                  <label htmlFor="bv-message" className="block text-sm font-medium mb-1.5">{t('modal.message')}</label>
                  <textarea id="bv-message" rows={3} value={form.message} onChange={e => setForm({ ...form, message: e.target.value })} className="w-full px-4 py-3 rounded-xl bg-background border border-border focus:border-secondary focus:ring-2 focus:ring-secondary/20 transition-all resize-none" />
                </div>
                {errorMsg && (
                  <p className="text-sm text-rose-600 bg-rose-50 border border-rose-200 rounded-lg px-3 py-2" role="alert">{errorMsg}</p>
                )}
                <div className="pt-4 flex gap-3">
                  <button type="button" onClick={handleClose} className="flex-1 min-h-[44px] py-3 px-4 rounded-xl border border-border font-medium hover:bg-muted transition-colors active:scale-[0.98]">
                    {t('modal.cancel')}
                  </button>
                  <button type="submit" disabled={status === 'submitting'} className="flex-[2] min-h-[44px] py-3 px-4 rounded-xl bg-secondary text-white font-semibold shadow-lg shadow-secondary/25 hover:bg-secondary/90 transition-all disabled:opacity-70 active:scale-[0.98]">
                    {status === 'submitting' ? t('general.loading') : t('modal.submit')}
                  </button>
                </div>
              </form>
            </>
          )}
        </div>
      </div>
    </div>
  );
}
