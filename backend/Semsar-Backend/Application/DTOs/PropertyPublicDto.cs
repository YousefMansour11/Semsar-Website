using System.Collections.Generic;

namespace Application.DTOs
{
    public class PropertyPublicDto : PropertyBaseDto
    {
        public string? Code { get; set; }
        // Public contact info is intentionally not exposed here; admin endpoints include full contact details
    }
}
