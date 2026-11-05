using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Jellyfin.Plugin.PandaBranding.Api;

/// <summary>
/// Sert les icônes. Public : le navigateur les demande avant toute connexion (page de connexion, onglet).
/// </summary>
[ApiController]
[Route("PandaBranding")]
public class BrandingController : ControllerBase
{
    [HttpGet("Icon")]
    [AllowAnonymous]
    public ActionResult Icon([FromQuery] int size = 192, [FromQuery] bool builtIn = false)
    {
        // L'adresse change avec l'icône (paramètre v) : on peut laisser le navigateur la garder longtemps.
        Response.Headers.CacheControl = "public, max-age=604800";
        return File(Branding.GetIcon(size, builtIn), "image/png");
    }
}
