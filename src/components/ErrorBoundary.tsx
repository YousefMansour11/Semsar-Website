import React from "react";
import { useLanguage } from "../i18n/LanguageContext";

interface Props {
  children: React.ReactNode;
}

interface State {
  hasError: boolean;
  error?: Error;
}

class ErrorBoundaryClass extends React.Component<Props & { t: (key: string, fb?: string) => string }, State> {
  state: State = { hasError: false };

  static getDerivedStateFromError(error: Error): State {
    return { hasError: true, error };
  }

  render() {
    if (this.state.hasError) {
      const { t } = this.props;
      return (
        <div className="min-h-screen flex items-center justify-center bg-background p-8">
          <div className="text-center space-y-4 max-w-lg">
            <h1 className="text-3xl sm:text-4xl font-bold text-foreground">{t('error.somethingWentWrong')}</h1>
            <p className="text-muted-foreground text-sm">{this.state.error?.message}</p>
            <button
              onClick={() => window.location.reload()}
              className="px-8 py-3.5 bg-gold text-navy rounded-xl font-semibold hover:bg-gold-dark hover:text-white transition-colors"
            >
              {t('error.reloadPage')}
            </button>
          </div>
        </div>
      );
    }
    return this.props.children;
  }
}

export default function ErrorBoundary(props: Props) {
  const { t } = useLanguage();
  return <ErrorBoundaryClass {...props} t={t} />;
}