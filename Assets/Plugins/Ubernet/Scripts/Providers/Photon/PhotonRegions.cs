namespace Skaillz.Ubernet.Providers.Photon
{
    public class PhotonRegions
    {
        public static string Europe = "eu";
        public static string US = "us";
        public static string Asia = "asia";
        public static string Japan = "jp";
        public static string Australia = "au";
        public static string USWest = "usw";
        public static string SouthAmerica = "sa";
        public static string CanadaEast = "cae";

        public static string[] AllRegions => new [] {
            Europe, US, Asia, Japan, Australia, USWest, SouthAmerica, CanadaEast
        };
        
        public static string[] DisplayNames => new [] {
            "Europe", "US", "Asia", "Japan", "Australia", "US West", "South America", "Canada East"
        };
    }
}