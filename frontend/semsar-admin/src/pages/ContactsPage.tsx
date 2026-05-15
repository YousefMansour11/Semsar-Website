import { useState, useEffect } from "react";
import { useStore } from "@/store";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Badge } from "@/components/ui/badge";
import {
  Table, TableBody, TableCell, TableHead, TableHeader, TableRow,
} from "@/components/ui/table";
import { Search, Phone, Users, Loader2 } from "lucide-react";

const CONTACT_BADGE: Record<string, string> = {
  Owner: 'bg-amber-100 text-amber-800 dark:bg-amber-900/30 dark:text-amber-400',
  Broker: 'bg-blue-100 text-blue-800 dark:bg-blue-900/30 dark:text-blue-400',
};

const CONTACT_TYPE_LABEL: Record<string | number, string> = {
  Owner: "Owner",
  Broker: "Broker",
};

export default function ContactsPage() {
  const contacts = useStore(s => s.contacts);

  const loadContacts = useStore(s => s.loadContacts);
  const [search, setSearch] = useState("");
  const [initialLoading, setInitialLoading] = useState(true);

  useEffect(() => {
    setInitialLoading(true);
    loadContacts().finally(() => setInitialLoading(false));
  }, [loadContacts]);

  const displayContacts = contacts.map(c => ({
    ...c,
    typeLabel: CONTACT_TYPE_LABEL[c.type] || "Owner",
  }));

  const filtered = displayContacts.filter((c) =>
    c.name.toLowerCase().includes(search.toLowerCase()) ||
    c.phone.includes(search)
  );

  return (
    <div className="space-y-6 animate-slide-in">
      <div>
        <h2 className="text-3xl font-bold tracking-tight">Contacts</h2>
        <p className="text-muted-foreground mt-1">{displayContacts.length} contacts</p>
      </div>

      <div className="relative max-w-sm">
        <Search className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-muted-foreground" />
        <Label htmlFor="contacts-search" className="sr-only">Search contacts</Label>
        <Input id="contacts-search" autoComplete="off" placeholder="Search by name or phone..." value={search} onChange={(e) => setSearch(e.target.value)} className="pl-10" />
      </div>

      {initialLoading ? (
        <div className="flex items-center justify-center py-20"><Loader2 className="w-8 h-8 animate-spin text-primary" /></div>
      ) : filtered.length === 0 ? (
        <div className="text-center py-20 border-2 border-dashed border-border rounded-2xl">
          <Users className="mx-auto h-12 w-12 text-muted-foreground/30 mb-4" />
          <h3 className="text-lg font-medium">No contacts found</h3>
        </div>
      ) : (
        <div className="border border-border rounded-xl overflow-x-auto">
          <Table>
            <caption className="sr-only">Contacts</caption>
            <TableHeader>
              <TableRow className="bg-accent/30">
                <TableHead>Name</TableHead>
                <TableHead>Phone</TableHead>
                <TableHead>Type</TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {filtered.map((c) => (
                <TableRow key={c.id}>
                  <TableCell className="font-medium">{c.name}</TableCell>
                  <TableCell>
                    <a href={`tel:${c.phone}`} className="flex items-center gap-1 text-sm hover:text-primary">
                      <Phone className="w-3 h-3" />{c.phone}
                    </a>
                  </TableCell>
                  <TableCell>
                    <Badge variant="outline" className={CONTACT_BADGE[c.typeLabel] || ''}>{c.typeLabel}</Badge>
                  </TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        </div>
      )}
    </div>
  );
}
