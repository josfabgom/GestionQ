namespace GestionQ.Web.Models
{
    public class UpdateManifest
    {
        public string Version { get; set; } = string.Empty;
        public string ReleaseDate { get; set; } = string.Empty;
        public string DownloadUrl { get; set; } = string.Empty;
        public string[] Changelog { get; set; } = System.Array.Empty<string>();
    }
}
