import { useState, useRef, useEffect } from 'react';
import { Header } from '../components/SiteHeader';
import { SiteFooter } from '../components/SiteFooter';
import SeoHelmet from '../components/SeoHelmet';
import { useLanguage } from '../i18n/LanguageContext';
import { submitLead, ApiError } from '../lib/api';
import { getTrackingFields } from '../lib/tracker';
import { onInteraction, resetInteraction, getHoneypotField } from '../lib/security';
import { useSettings } from '../hooks/use-settings';
import { getSiteUrl } from '../lib/paths';
import { Phone, Mail, MapPin, CheckCircle2, Loader2, Clock } from 'lucide-react';

export default function ContactPage() {
  const { t } = useLanguage();
  const { data: settings } = useSettings();
  const phoneNumber = settings?.phoneNumber || '+201558730895';
  const email = settings?.email || 'semsar.realestate@gmail.com';
  const [status, setStatus] = useState<'idle' | 'submitting' | 'success'>('idle');
  const [form, setForm] = useState({ name: '', phone: '', message: '' });
  const [errorMsg, setErrorMsg] = useState<string | null>(null);
  const [cooldownRemaining, setCooldownRemaining] = useState(0);
  const submittingRef = useRef(false);
  const formStartTime = useRef(Date.now());
  const [hpField] = useState(() => getHoneypotField());
  const cooldownTimerRef = useRef<ReturnType<typeof setInterval>>();

  useEffect(() => {
    resetInteraction();
    const interact = () => onInteraction();
    window.addEventListener('scroll', interact, { once: true });
    return () => window.removeEventListener('scroll', interact);
  }, []);

  useEffect(() => {
    if (cooldownRemaining <= 0) {
      if (cooldownTimerRef.current) clearInterval(cooldownTimerRef.current);
      return;
    }
    cooldownTimerRef.current = setInterval(() => {
      setCooldownRemaining(prev => {
        if (prev <= 1) {
          if (cooldownTimerRef.current) clearInterval(cooldownTimerRef.current);
          return 0;
        }
        return prev - 1;
      });
    }, 1000);
    return () => { if (cooldownTimerRef.current) clearInterval(cooldownTimerRef.current); };
  }, [cooldownRemaining]);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (submittingRef.current) return;
    if (cooldownRemaining > 0) return;
    if (Date.now() - formStartTime.current < 3000) {
      setErrorMsg(t('contact.formError'));
      return;
    }
    submittingRef.current = true;
    setStatus('submitting');
    setErrorMsg(null);
    const phoneDigits = form.phone.replace(/[^\d+]/g, '');
    if (phoneDigits.length < 8) {
      setErrorMsg(t('contact.invalidPhone'));
      setStatus('idle');
      submittingRef.current = false;
      return;
    }
    try {
      await submitLead({ name: form.name, phone: phoneDigits, message: form.message || undefined, ...getTrackingFields() });
      setStatus('success');
    } catch (err) {
      if (err instanceof ApiError && err.retryAfterMs) {
        setCooldownRemaining(Math.ceil(err.retryAfterMs / 1000));
      }
      setErrorMsg(err instanceof ApiError ? err.message : t('contact.formError'));
      setStatus('idle');
    } finally {
      submittingRef.current = false;
    }
  };

  const inputCls = "w-full px-4 py-3 rounded-xl bg-background border border-border focus:border-secondary focus:ring-2 focus:ring-secondary/20 transition-all will-change-transform";

  return (
    <div className="min-h-screen bg-background pt-20">
      <SeoHelmet
        title={t('seo.contactTitle')}
        description={t('seo.contactDescription')}
        canonical={typeof window !== 'undefined' ? window.location.href : undefined}
        alternates={[
          { hrefLang: 'en', href: `${getSiteUrl()}/en/contact` },
          { hrefLang: 'ar', href: `${getSiteUrl()}/ar/contact` },
        ]}
      />
      <Header />

      <div className="max-w-5xl mx-auto px-4 sm:px-6 lg:px-8 py-12 sm:py-24">
        <h1 className="font-display text-3xl sm:text-5xl font-bold text-foreground mb-4">{t('contact.heading')}</h1>
        <div className="w-16 h-1.5 bg-gold rounded-full mb-12" />

        <div className="grid grid-cols-1 lg:grid-cols-2 gap-12">
          <div className="space-y-8">
            <div className="space-y-4">
              <div className="flex items-center gap-4">
                <div className="w-12 h-12 rounded-xl bg-gold/10 flex items-center justify-center text-amber-600 shrink-0">
                  <Phone className="w-5 h-5" />
                </div>
                <div>
                  <p className="text-sm text-muted-foreground">{t('contact.phone')}</p>
                  <a href={`tel:${phoneNumber}`} className="font-semibold hover:text-primary transition-colors" dir="ltr">{phoneNumber}</a>
                </div>
              </div>
              <div className="flex items-center gap-4">
                <div className="w-12 h-12 rounded-xl bg-gold/10 flex items-center justify-center text-amber-600 shrink-0">
                  <Mail className="w-5 h-5" />
                </div>
                <div>
                  <p className="text-sm text-muted-foreground">{t('contact.email')}</p>
                  <a href={`mailto:${email}`} className="font-semibold hover:text-primary transition-colors">{email}</a>
                </div>
              </div>
              <div className="flex items-center gap-4">
                <div className="w-12 h-12 rounded-xl bg-gold/10 flex items-center justify-center text-amber-600 shrink-0">
                  <MapPin className="w-5 h-5" />
                </div>
                <div>
                  <p className="text-sm text-muted-foreground">{t('contact.address')}</p>
                  <p className="font-semibold">{t('contact.addressValue')}</p>
                </div>
              </div>
            </div>
          </div>

          <div className="bg-card border border-border rounded-2xl p-6 sm:p-8">
            {status === 'success' ? (
              <div className="text-center py-8">
                <div className="w-16 h-16 bg-green-100 text-green-600 rounded-full flex items-center justify-center mx-auto mb-6">
                  <CheckCircle2 className="w-8 h-8" />
                </div>
                <h2 className="text-2xl font-display font-bold mb-2">{t('contact.successTitle')}</h2>
                <p className="text-muted-foreground">{t('contact.successDesc')}</p>
              </div>
            ) : (
              <form onSubmit={handleSubmit} autoComplete="off" className="space-y-4">
                <input type="text" name={hpField.name} tabIndex={-1} autoComplete="off" className="absolute opacity-0 pointer-events-none h-0 w-0" aria-hidden="true" defaultValue={hpField.value} />
                <h2 className="text-xl font-bold mb-4">{t('contact.formTitle')}</h2>
                <div>
                  <label htmlFor="ct-name" className="block text-sm font-medium mb-1.5">{t('contact.formName')}</label>
                  <input id="ct-name" type="text" autoComplete="name" required value={form.name} onFocus={onInteraction} onKeyDown={onInteraction} onChange={e => { onInteraction(); setForm({ ...form, name: e.target.value }); }} className={inputCls} />
                </div>
                <div>
                  <label htmlFor="ct-phone" className="block text-sm font-medium mb-1.5">{t('contact.formPhone')}</label>
                  <input id="ct-phone" type="tel" autoComplete="tel" required dir="ltr" value={form.phone} onFocus={onInteraction} onKeyDown={onInteraction} onChange={e => { onInteraction(); setForm({ ...form, phone: e.target.value }); }} className={inputCls} placeholder={t('general.placeholderPhone')} />
                </div>
                <div>
                  <label htmlFor="ct-message" className="block text-sm font-medium mb-1.5">{t('contact.formMessage')}</label>
                  <textarea id="ct-message" rows={4} maxLength={2000} value={form.message} onFocus={onInteraction} onKeyDown={onInteraction} onChange={e => { onInteraction(); setForm({ ...form, message: e.target.value }); }} className={`${inputCls} resize-none`} />
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
                <button type="submit" disabled={status === 'submitting' || cooldownRemaining > 0} className="w-full min-h-[48px] py-3 bg-gold text-navy rounded-xl font-bold shadow-lg shadow-amber-900/20 hover:bg-gold-dark hover:text-white hover:shadow-xl transition-all duration-200 disabled:opacity-70 active:scale-[0.97] will-change-transform">
                  {status === 'submitting' ? <Loader2 className="w-5 h-5 animate-spin mx-auto" /> : cooldownRemaining > 0 ? t('general.waiting') : t('contact.formSubmit')}
                </button>
              </form>
            )}
          </div>
        </div>
      </div>

      <SiteFooter />
    </div>
  );
}