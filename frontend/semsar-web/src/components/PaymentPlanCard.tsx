import { useLanguage } from '../i18n/LanguageContext';
import { CreditCard, Calendar, Wallet, PiggyBank } from 'lucide-react';
import type { InstallmentPlan } from '../types/property';

interface PaymentPlanCardProps {
  plan: InstallmentPlan;
  basePrice: number;
  currency: string;
  variantSize?: number;
  planIndex: number;
  totalPlans: number;
}

export function PaymentPlanCard({ plan, basePrice, currency, variantSize, planIndex, totalPlans }: PaymentPlanCardProps) {
  const { t, fmtPrice, fmtNum } = useLanguage();

  const downPaymentAmount = basePrice * (plan.downPaymentPercent / 100);
  const totalMonths = plan.installmentMonths || (plan.years * 12);
  const monthlyAmount =
    plan.monthlyAmount && plan.monthlyAmount > 0
      ? plan.monthlyAmount
      : (basePrice - downPaymentAmount) / totalMonths;

  return (
    <div className="bg-white rounded-2xl border border-gold/20 overflow-hidden shadow-sm hover:shadow-md transition-shadow">
      {totalPlans > 1 && (
        <div className="bg-gradient-to-r from-gold/10 to-gold/5 px-5 py-2 border-b border-gold/10">
          <span className="text-xs font-bold text-gold uppercase tracking-wider">{t('installment.plan')} {planIndex + 1}</span>
          {variantSize != null && (
            <span className="text-xs text-muted-foreground ml-3">{fmtNum(variantSize)} {t('general.m2')}</span>
          )}
        </div>
      )}

      {plan.paymentType === 'Cash' ? (
        <div className="p-5 text-center">
          <div className="w-12 h-12 rounded-2xl bg-gold/10 flex items-center justify-center mx-auto mb-3">
            <Wallet className="w-6 h-6 text-gold" />
          </div>
          <div className="text-sm text-muted-foreground mb-1">{t('installment.cashPrice')}</div>
          <div className="font-bold text-navy text-2xl mb-1">
            {fmtPrice(Math.round(basePrice * (1 - (plan.discountPercent ?? 0) / 100)), currency)}
          </div>
          {(plan.discountPercent ?? 0) > 0 && (
            <div className="inline-flex items-center gap-1 px-3 py-1 rounded-full bg-emerald-500/10 text-emerald-600 text-xs font-semibold">
              <PiggyBank className="w-3.5 h-3.5" />
              {fmtNum(plan.discountPercent ?? 0)}% {t('installment.discount')}
            </div>
          )}
        </div>
      ) : (
        <div className="p-5">
          <div className="grid grid-cols-3 gap-3">
            <div className="text-center">
              <div className="w-10 h-10 rounded-xl bg-gold/5 flex items-center justify-center mx-auto mb-2">
                <CreditCard className="w-5 h-5 text-gold" />
              </div>
              <div className="text-[10px] text-muted-foreground uppercase tracking-wide mb-0.5">{t('installment.downPayment')}</div>
              <div className="font-bold text-navy text-lg">{fmtNum(plan.downPaymentPercent)}%</div>
              <div className="text-[11px] text-muted-foreground">{fmtPrice(downPaymentAmount, currency)}</div>
            </div>

            <div className="text-center">
              <div className="w-10 h-10 rounded-xl bg-gold/5 flex items-center justify-center mx-auto mb-2">
                <Calendar className="w-5 h-5 text-gold" />
              </div>
              <div className="text-[10px] text-muted-foreground uppercase tracking-wide mb-0.5">{t('installment.years')}</div>
              <div className="font-bold text-navy text-lg">{fmtNum(plan.years)}</div>
              <div className="text-[11px] text-muted-foreground">{fmtNum(plan.installmentMonths || plan.years * 12)} {t('general.months')}</div>
            </div>

            <div className="text-center">
              <div className="w-10 h-10 rounded-xl bg-gold/5 flex items-center justify-center mx-auto mb-2">
                <Wallet className="w-5 h-5 text-gold" />
              </div>
              <div className="text-[10px] text-muted-foreground uppercase tracking-wide mb-0.5">{t('installment.monthly')}</div>
              <div className="font-bold text-navy text-lg">{fmtPrice(monthlyAmount, currency)}</div>
              <div className="text-[11px] text-muted-foreground">{t('installment.perMonth')}</div>
            </div>
          </div>

          {(plan.quarterlyAmount != null && plan.quarterlyAmount > 0) && (
            <div className="mt-3 pt-3 border-t border-border/50 text-center">
              <span className="text-[11px] text-muted-foreground">
                Quarterly: {fmtPrice(plan.quarterlyAmount, currency)}
              </span>
            </div>
          )}
        </div>
      )}
    </div>
  );
}
