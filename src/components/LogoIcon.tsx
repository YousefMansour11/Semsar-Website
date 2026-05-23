interface LogoIconProps {
  variant?: 'navy' | 'gold';
}

export function LogoIcon({ variant = 'navy' }: LogoIconProps) {
  if (variant === 'gold') {
    return (
      <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 80 80" fill="none" className="w-full h-full">
        <rect x="0" y="75.5" width="80" height="0.8" rx="0.4" fill="#B5934A" opacity="0.28"/>
        <rect x="1" y="51" width="17" height="24" rx="2" fill="#B5934A"/>
        <rect x="1" y="51" width="17" height="1.8" rx="1" fill="#B5934A" opacity="0.55"/>
        <rect x="3.5" y="57" width="4.5" height="7" rx="1.5" fill="white" opacity="0.82"/>
        <rect x="10" y="57" width="4.5" height="7" rx="1.5" fill="white" opacity="0.82"/>
        <rect x="3.5" y="67" width="4.5" height="5.5" rx="1.5" fill="white" opacity="0.82"/>
        <rect x="10" y="67" width="4.5" height="5.5" rx="1.5" fill="white" opacity="0.82"/>
        <rect x="20" y="29" width="22" height="46" rx="2.5" fill="#B5934A"/>
        <rect x="20" y="29" width="22" height="2" rx="1" fill="#B5934A" opacity="0.55"/>
        <rect x="23" y="36" width="7" height="6.5" rx="1.3" fill="white" opacity="0.82"/>
        <rect x="33" y="36" width="7" height="6.5" rx="1.3" fill="white" opacity="0.82"/>
        <rect x="23" y="46" width="7" height="6.5" rx="1.3" fill="white" opacity="0.82"/>
        <rect x="33" y="46" width="7" height="6.5" rx="1.3" fill="white" opacity="0.82"/>
        <rect x="23" y="56" width="7" height="6.5" rx="1.3" fill="white" opacity="0.82"/>
        <rect x="33" y="56" width="7" height="6.5" rx="1.3" fill="white" opacity="0.82"/>
        <rect x="46" y="10" width="30" height="65" rx="2.5" fill="#B5934A"/>
        <rect x="46" y="10" width="30" height="2.2" rx="1.1" fill="#B5934A" opacity="0.55"/>
        <rect x="49.5" y="16.5" width="9" height="6.5" rx="1.2" fill="white" opacity="0.82"/>
        <rect x="63" y="16.5" width="9" height="6.5" rx="1.2" fill="white" opacity="0.82"/>
        <rect x="49.5" y="27" width="9" height="6.5" rx="1.2" fill="white" opacity="0.82"/>
        <rect x="63" y="27" width="9" height="6.5" rx="1.2" fill="white" opacity="0.82"/>
        <rect x="49.5" y="37.5" width="9" height="6.5" rx="1.2" fill="white" opacity="0.82"/>
        <rect x="63" y="37.5" width="9" height="6.5" rx="1.2" fill="white" opacity="0.82"/>
        <rect x="49.5" y="48" width="9" height="6.5" rx="1.2" fill="white" opacity="0.82"/>
        <rect x="63" y="48" width="9" height="6.5" rx="1.2" fill="white" opacity="0.82"/>
      </svg>
    );
  }

  return (
    <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 80 80" fill="none" className="w-full h-full">
      <rect x="0" y="75.5" width="80" height="0.8" rx="0.4" fill="#0A1628" opacity="0.28"/>
      <rect x="1" y="51" width="17" height="24" rx="2" fill="#0A1628"/>
      <rect x="1" y="51" width="17" height="1.8" rx="1" fill="#0A1628" opacity="0.55"/>
      <rect x="3.5" y="57" width="4.5" height="7" rx="1.5" fill="#B5934A" opacity="0.88"/>
      <rect x="10" y="57" width="4.5" height="7" rx="1.5" fill="#B5934A" opacity="0.88"/>
      <rect x="3.5" y="67" width="4.5" height="5.5" rx="1.5" fill="#B5934A" opacity="0.88"/>
      <rect x="10" y="67" width="4.5" height="5.5" rx="1.5" fill="#B5934A" opacity="0.88"/>
      <rect x="20" y="29" width="22" height="46" rx="2.5" fill="#0A1628"/>
      <rect x="20" y="29" width="22" height="2" rx="1" fill="#0A1628" opacity="0.55"/>
      <rect x="23" y="36" width="7" height="6.5" rx="1.3" fill="#B5934A" opacity="0.88"/>
      <rect x="33" y="36" width="7" height="6.5" rx="1.3" fill="#B5934A" opacity="0.88"/>
      <rect x="23" y="46" width="7" height="6.5" rx="1.3" fill="#B5934A" opacity="0.88"/>
      <rect x="33" y="46" width="7" height="6.5" rx="1.3" fill="#B5934A" opacity="0.88"/>
      <rect x="23" y="56" width="7" height="6.5" rx="1.3" fill="#B5934A" opacity="0.88"/>
      <rect x="33" y="56" width="7" height="6.5" rx="1.3" fill="#B5934A" opacity="0.88"/>
      <rect x="46" y="10" width="30" height="65" rx="2.5" fill="#0A1628"/>
      <rect x="46" y="10" width="30" height="2.2" rx="1.1" fill="#0A1628" opacity="0.55"/>
      <rect x="49.5" y="16.5" width="9" height="6.5" rx="1.2" fill="#B5934A" opacity="0.88"/>
      <rect x="63" y="16.5" width="9" height="6.5" rx="1.2" fill="#B5934A" opacity="0.88"/>
      <rect x="49.5" y="27" width="9" height="6.5" rx="1.2" fill="#B5934A" opacity="0.88"/>
      <rect x="63" y="27" width="9" height="6.5" rx="1.2" fill="#B5934A" opacity="0.88"/>
      <rect x="49.5" y="37.5" width="9" height="6.5" rx="1.2" fill="#B5934A" opacity="0.88"/>
      <rect x="63" y="37.5" width="9" height="6.5" rx="1.2" fill="#B5934A" opacity="0.88"/>
      <rect x="49.5" y="48" width="9" height="6.5" rx="1.2" fill="#B5934A" opacity="0.88"/>
      <rect x="63" y="48" width="9" height="6.5" rx="1.2" fill="#B5934A" opacity="0.88"/>
    </svg>
  );
}
