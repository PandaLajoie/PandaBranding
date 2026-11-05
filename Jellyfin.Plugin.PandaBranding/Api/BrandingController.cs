using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Jellyfin.Plugin.PandaBranding.Api;

/// <summary>
/// Sert l'icône choisie par l'administrateur. Public : le navigateur la demande avant toute connexion (page de
/// connexion, onglet).
/// </summary>
[ApiController]
[Route("PandaBranding")]
public class BrandingController : ControllerBase
{
    /// <param name="size">Taille demandée (dans l'adresse pour le navigateur ; la même image sert à toutes).</param>
    [HttpGet("Icon")]
    [AllowAnonymous]
    public ActionResult Icon([FromQuery] int size = 192)
    {
        var icon = Branding.GetIcon();
        if (icon == null)
        {
            return NotFound();
        }

        // L'adresse change avec l'icône (paramètre v) : on peut laisser le navigateur la garder longtemps.
        Response.Headers.CacheControl = "public, max-age=604800";
        return File(icon, "image/png");
    }
}
