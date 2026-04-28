import React, { useState, useCallback, useRef, useEffect } from 'react';
import { X, CheckCircle2, Clock } from 'lucide-react';
import { useLanguage } from '../i18n/LanguageContext';
import type { BookingPayload } from '../types/property';
import { submitBooking, ApiError } from '../lib/api';
import { getTrackingFields } from '../lib/tracker';
import { onInteraction, resetInteraction, getHoneypotField } from '../lib/security';
import { useFocusTrap } from '../lib/use-focus-trap';

interface BookViewingModalProps {
  isOpen: boolean;
  onClose: () => void;
  // Backend booking distinguishes property vs unit. Caller passes one of these.
  propertyId?: number | null;
  unitId?: number | null;
  // Variant info captured with the lead
  variantName?: string;
  variantSize?: number;
  variantPrice?: number;
  variantPublicKey?: string;
  projectId?: string | null;
  unitType?: string;
  propertyCode?: string;
}

export function BookViewingModal({ isOpen, onClose, propertyId = null, unitId = null, variantName, variantSize, variantPublicKey, projectId, unitType, propertyCode }: BookViewingModalProps) {
  const { t, language } = useLanguage();
  const [status, setStatus] = useState<'idle' | 'submitting' | 'success'>('idle');
  const [form, setForm] = useState({ name: '', phone: '', message: '' });

  const [errorMsg, setErrorMsg] = useState<string | null>(null);
  const [cooldownRemaining, setCooldownRemaining] = useState(0);
  const submittingRef = useRef(false);
  const formStartTime = useRef(Date.now());
  const [hpField] = useState(() => getHoneypotField());
  const cooldownTimerRef = useRef<ReturnType<typeof setInterval>>();
  const focusTrapRef = useFocusTrap(isOpen);

  useEffect(() => {
    if (cooldownRemaining <= 0) {
      if (cooldownTimerRef.current) { clearInterval(cooldownTimerRef.current); cooldownTimerRef.current = undefined; }
      return;
    }
    cooldownTimerRef.current = setInterval(() => {
      setCooldownRemaining(prev => {
        if (prev <= 1) {
          clearInterval(cooldownTimerRef.current);
          cooldownTimerRef.current = undefined;
          return 0;
        }
        return prev - 1;
      });
    }, 1000);
    return () => { if (cooldownTimerRef.current) { clearInterval(cooldownTimerRef.current); cooldownTimerRef.current = undefined; } };
  }, [cooldownRemaining]);

  useEffect(() => {
    if (isOpen) submittingRef.current = false;
  }, [isOpen]);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (submittingRef.current) return;
    if (cooldownRemaining > 0) return;
    if (Date.now() - formStartTime.current < 3000) {
      setErrorMsg(t('error.submitFailed'));
      return;
    }
    submittingRef.current = true;
    setStatus('submitting');
    setErrorMsg(null);

    const safeId = (id: number | null | undefined) => (id != null && id > 0 && !isNaN(id) ? id : null);
    const uId = safeId(unitId);
    const pId = safeId(propertyId);
    if (!uId && !pId) {
      setErrorMsg(t('error.submitFailed'));
      setStatus('idle');
      submittingRef.current = false;
      return;
    }
    const safeStr = (s: string) => s.replace(/[<>]/g, '');
    const variantInfo = `\n[${propertyCode ? `Code: ${propertyCode}, ` : ''}${projectId ? `Project: ${projectId}, ` : ''}${unitType ? `Type: ${unitType}, ` : ''}${variantName ? `Variant: ${safeStr(variantName)}` : ''}${variantSize ? `, ${variantSize} sqm` : ''}${variantPublicKey ? `, ID: ${variantPublicKey}` : ''}]`;
    const payload: BookingPayload = {
      propertyId: pId,
      unitId: uId,
      name: form.name,
      phone: form.phone,
      message: (form.message || '') + variantInfo || undefined,
      ...getTrackingFields(),
    };

    const phoneDigits = form.phone.replace(/[^\d+]/g, '');
    if (phoneDigits.length < 8) {
      setErrorMsg(t('form.error.invalidPhone'));
      setStatus('idle');
      submittingRef.current = false;
      return;
    }
    try {
      await submitBooking({ ...payload, phone: phoneDigits }, new Date(formStartTime.current).toISOString());
      setStatus('success');
    } catch (err) {
      if (err instanceof ApiError && err.retryAfterMs) {
        setCooldownRemaining(Math.ceil(err.retryAfterMs / 1000));
      }
      const msg = err instanceof ApiError ? err.message : t('error.submitFailed');
      setErrorMsg(msg);
      setStatus('idle');
    } finally {
      submittingRef.current = false;
    }
  };

  const handleClose = useCallback(() => { setStatus('idle'); setForm({ name: '', phone: '', message: '' }); setErrorMsg(null); setCooldownRemaining(0); onClose(); }, [onClose]);

  React.useEffect(() => {
    if (!isOpen) return;
    formStartTime.current = Date.now();
    resetInteraction();
    const prev = document.activeElement as HTMLElement | null;
    const handler = (e: KeyboardEvent) => { if (e.key === 'Escape') handleClose(); };
    window.addEventListener('keydown', handler);
    const interact = () => onInteraction();
    window.addEventListener('scroll', interact, { once: true });
    return () => {
      window.removeEventListener('keydown', handler);
      window.removeEventListener('scroll', interact);
      prev?.focus();
    };
  }, [isOpen, handleClose]);

  if (!isOpen) return null;

  return (
    <div ref={focusTrapRef} className="fixed inset-0 z-50 flex items-center justify-center p-4 animate-in fade-in duration-200" role="dialog" aria-modal="true" aria-label={t('modal.title')}>
      <div className="absolute inset-0 bg-navy/70 backdrop-blur-md" onClick={handleClose} aria-hidden="true" />
      <div className="relative w-full max-w-md bg-white rounded-2xl shadow-2xl shadow-black/10 overflow-hidden animate-in zoom-in-95 duration-200">
        <button type="button" onClick={handleClose} aria-label={t('modal.close')} className={`absolute top-4 ${language === 'ar' ? 'left-4' : 'right-4'} text-muted-foreground hover:text-foreground p-2 rounded-full hover:bg-muted z-10`} autoFocus>
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
                <input type="text" name={hpField.name} tabIndex={-1} autoComplete="off" className="absolute opacity-0 pointer-events-none h-0 w-0" aria-hidden="true" defaultValue={hpField.value} />
                <div>
                  <label htmlFor="bv-name" className="block text-sm font-medium mb-1.5">{t('modal.name')}</label>
                  <input id="bv-name" type="text" autoComplete="name" required value={form.name} onFocus={onInteraction} onKeyDown={onInteraction} onChange={e => { onInteraction(); setForm({ ...form, name: e.target.value }); }} className="w-full px-4 py-3 rounded-xl bg-background border border-border focus:border-secondary focus:ring-2 focus:ring-secondary/20 transition-shadow" />
                </div>
                <div>
                  <label htmlFor="bv-phone" className="block text-sm font-medium mb-1.5">{t('modal.phone')}</label>
                  <input id="bv-phone" type="tel" autoComplete="tel" required dir="ltr" value={form.phone} onFocus={onInteraction} onKeyDown={onInteraction} onChange={e => { onInteraction(); setForm({ ...form, phone: e.target.value }); }} className="w-full px-4 py-3 rounded-xl bg-background border border-border focus:border-secondary focus:ring-2 focus:ring-secondary/20 transition-shadow" placeholder="+20 100 000 0000" />
                </div>
                <div>
                  <label htmlFor="bv-message" className="block text-sm font-medium mb-1.5">{t('modal.message')}</label>
                  <textarea id="bv-message" rows={3} value={form.message} onFocus={onInteraction} onKeyDown={onInteraction} onChange={e => { onInteraction(); setForm({ ...form, message: e.target.value }); }} className="w-full px-4 py-3 rounded-xl bg-background border border-border focus:border-secondary focus:ring-2 focus:ring-secondary/20 transition-shadow resize-none" />
                </div>
                {errorMsg && !cooldownRemaining && (
                  <p className="text-sm text-rose-600 bg-rose-50 border border-rose-200 rounded-lg px-3 py-2" role="alert">{errorMsg}</p>
                )}
                {cooldownRemaining > 0 && (
                  <p className="text-sm text-amber-700 bg-amber-50 border border-amber-200 rounded-lg px-3 py-2 flex items-center gap-2" role="alert">
                    <Clock className="w-4 h-4 shrink-0" />
                    {t('error.cooldown', undefined, { seconds: String(cooldownRemaining) })}
                  </p>
                )}
                <div className="pt-4 flex gap-3">
                  <button type="button" onClick={handleClose} className="flex-1 min-h-[44px] py-3 px-4 rounded-xl border border-border font-medium hover:bg-muted transition-colors active:scale-[0.98]">
                    {t('modal.cancel')}
                  </button>
                  <button type="submit" disabled={status === 'submitting' || cooldownRemaining > 0} className="flex-[2] min-h-[44px] py-3 px-4 rounded-xl bg-secondary text-white font-semibold shadow-lg shadow-secondary/25 hover:bg-secondary/90 transition-colors disabled:opacity-70 active:scale-[0.98]">
                    {status === 'submitting' ? t('general.loading') : cooldownRemaining > 0 ? t('general.waiting') : t('modal.submit')}
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
