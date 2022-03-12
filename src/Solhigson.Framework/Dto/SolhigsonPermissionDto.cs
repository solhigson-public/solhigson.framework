using System.Collections.Generic;

namespace Solhigson.Framework.Dto;

public record SolhigsonPermissionDto
{
    public string Name { get; set; }
    
    public string Description { get; set; }

    public string Url { get; set; }
        
    public string Icon { get; set; }

    public string OnClickFunction { get; set; }
        
    public bool IsMenu { get; set; }

    public virtual List<SolhigsonPermissionDto> Children { get; set; }

}