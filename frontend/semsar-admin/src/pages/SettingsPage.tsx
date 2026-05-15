import { useState, useEffect } from "react";
import { toast } from "sonner";
import { adminApi } from "@/lib/admin-api";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Loader2, Save } from "lucide-react";

export default function SettingsPage() {
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [form, setForm] = useState({
    companyName: '',
    whatsappNumber: '',
    phoneNumber: '',
    facebook: '',
    instagram: '',
    tiktok: '',
  });

  useEffect(() => {
    adminApi.getSettings()
      .then((s) => setForm({
        companyName: s.companyName || '',
        whatsappNumber: s.whatsappNumber || '',
        phoneNumber: s.phoneNumber || '',
        facebook: s.socialLinks?.facebook || '',
        instagram: s.socialLinks?.instagram || '',
        tiktok: s.socialLinks?.tiktok || '',
      }))
      .catch(() => toast.error("Failed to load settings"))
      .finally(() => setLoading(false));
  }, []);

  const handleSave = async () => {
    setSaving(true);
    try {
      await adminApi.updateSettings({
        companyName: form.companyName,
        whatsappNumber: form.whatsappNumber,
        phoneNumber: form.phoneNumber,
        socialLinks: {
          facebook: form.facebook || undefined,
          instagram: form.instagram || undefined,
          tiktok: form.tiktok || undefined,
        },
      });
      toast.success("Settings saved");
    } catch {
      toast.error("Failed to save settings");
    } finally {
      setSaving(false);
    }
  };

  if (loading) {
    return <div className="flex items-center justify-center py-20"><Loader2 className="w-8 h-8 animate-spin text-primary" /></div>;
  }

  return (
    <div className="space-y-6 animate-slide-in max-w-2xl">
      <div>
        <h2 className="text-3xl font-bold tracking-tight">Settings</h2>
        <p className="text-muted-foreground mt-1">Manage site-wide configuration.</p>
      </div>

      <Card className="bg-card border-border">
        <CardHeader>
          <CardTitle>Company Info</CardTitle>
        </CardHeader>
        <CardContent className="space-y-4">
          <div className="space-y-2">
            <Label htmlFor="settings-company-name">Company Name</Label>
            <Input id="settings-company-name" autoComplete="organization" value={form.companyName} onChange={(e) => setForm({ ...form, companyName: e.target.value })} />
          </div>
          <div className="space-y-2">
            <Label htmlFor="settings-whatsapp">WhatsApp Number</Label>
            <Input id="settings-whatsapp" autoComplete="tel" value={form.whatsappNumber} onChange={(e) => setForm({ ...form, whatsappNumber: e.target.value })} placeholder="+201234567890" />
          </div>
          <div className="space-y-2">
            <Label htmlFor="settings-phone">Phone Number</Label>
            <Input id="settings-phone" autoComplete="tel" value={form.phoneNumber} onChange={(e) => setForm({ ...form, phoneNumber: e.target.value })} placeholder="+201234567890" />
          </div>
        </CardContent>
      </Card>

      <Card className="bg-card border-border">
        <CardHeader>
          <CardTitle>Social Media Links</CardTitle>
        </CardHeader>
        <CardContent className="space-y-4">
          <div className="space-y-2">
            <Label htmlFor="settings-facebook">Facebook URL</Label>
            <Input id="settings-facebook" autoComplete="url" value={form.facebook} onChange={(e) => setForm({ ...form, facebook: e.target.value })} placeholder="https://facebook.com/..." />
          </div>
          <div className="space-y-2">
            <Label htmlFor="settings-instagram">Instagram URL</Label>
            <Input id="settings-instagram" autoComplete="url" value={form.instagram} onChange={(e) => setForm({ ...form, instagram: e.target.value })} placeholder="https://instagram.com/..." />
          </div>
          <div className="space-y-2">
            <Label htmlFor="settings-tiktok">TikTok URL</Label>
            <Input id="settings-tiktok" autoComplete="url" value={form.tiktok} onChange={(e) => setForm({ ...form, tiktok: e.target.value })} placeholder="https://tiktok.com/..." />
          </div>
        </CardContent>
      </Card>

      <Button onClick={handleSave} disabled={saving} className="bg-primary hover:bg-primary/90">
        <Save className="mr-2 h-4 w-4" /> {saving ? "Saving..." : "Save Settings"}
      </Button>
    </div>
  );
}
