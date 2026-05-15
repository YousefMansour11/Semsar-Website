import { useCallback } from 'react';

interface Props {
  governorate: string;
  city: string;
  area: string;
  onChange: (governorate: string, city: string, area: string, combined: string) => void;
}

export function AdminLocationPicker({ governorate, city, area, onChange }: Props) {
  const combine = useCallback((gov: string, cty: string, are: string) => {
    return [gov, cty, are].filter(Boolean).join(', ');
  }, []);

  const handleGovernorateChange = useCallback(
    (e: React.ChangeEvent<HTMLInputElement>) => onChange(e.target.value, city, area, combine(e.target.value, city, area)),
    [city, area, onChange, combine]
  );

  const handleCityChange = useCallback(
    (e: React.ChangeEvent<HTMLInputElement>) => onChange(governorate, e.target.value, area, combine(governorate, e.target.value, area)),
    [governorate, area, onChange, combine]
  );

  const handleAreaChange = useCallback(
    (e: React.ChangeEvent<HTMLInputElement>) => onChange(governorate, city, e.target.value, combine(governorate, city, e.target.value)),
    [governorate, city, onChange, combine]
  );

  return (
    <div className="grid grid-cols-3 gap-3">
      <div className="space-y-1.5">
        <label htmlFor="alp-governorate" className="text-xs font-medium text-muted-foreground">Governorate</label>
        <input id="alp-governorate" name="governorate"
          type="text"
          placeholder="e.g. RedSea"
          value={governorate}
          onChange={handleGovernorateChange}
          className="w-full border border-input bg-background rounded-md px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-ring"
        />
      </div>
      <div className="space-y-1.5">
        <label htmlFor="alp-city" className="text-xs font-medium text-muted-foreground">City</label>
        <input id="alp-city" name="city"
          type="text"
          placeholder="e.g. Hurghada"
          value={city}
          onChange={handleCityChange}
          className="w-full border border-input bg-background rounded-md px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-ring"
        />
      </div>
      <div className="space-y-1.5">
        <label htmlFor="alp-area" className="text-xs font-medium text-muted-foreground">Area</label>
        <input id="alp-area" name="area"
          type="text"
          placeholder="e.g. Dahar"
          value={area}
          onChange={handleAreaChange}
          className="w-full border border-input bg-background rounded-md px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-ring"
        />
      </div>
    </div>
  );
}