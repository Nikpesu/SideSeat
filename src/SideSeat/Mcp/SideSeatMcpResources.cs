using System.ComponentModel;
using ModelContextProtocol.Server;

namespace SideSeat.Mcp;

[McpServerResourceType]
public static class SideSeatMcpResources
{
    [McpServerResource(
        UriTemplate = "sideseat://sitemap",
        Name = "sideseat-sitemap",
        MimeType = "application/json")]
    [Description("Role-independent SideSeat navigation and endpoint overview.")]
    public static string Sitemap() =>
        """
        {
          "public":["/","/Home/Privacy"],
          "authenticated":["/Voznja","/Rezervacija","/Ocjena","/Korisnik/Saldo","/Korisnik/Settings"],
          "driver":["/Voznja/Create","/Voznja?view=driving","/Rezervacija?view=my-rides"],
          "admin":["/Grad","/Korisnik","/Vozilo","/Placanje","/Audit"],
          "mcp":"/mcp"
        }
        """;

    [McpServerResource(
        UriTemplate = "sideseat://api-summary",
        Name = "sideseat-api-summary",
        MimeType = "application/json")]
    [Description("Summary of SideSeat API and MCP capabilities.")]
    public static string ApiSummary() =>
        """
        {
          "rest":["gradovi","korisnici","vozila","voznje","rezervacije","placanja","ocjene","saldo-transakcije","search","ai"],
          "writeFlow":"Call prepare_create_* and then confirm_action with the returned token.",
          "safety":"All results are role-filtered. Write actions are audited and confirmation tokens are single-use."
        }
        """;
}
