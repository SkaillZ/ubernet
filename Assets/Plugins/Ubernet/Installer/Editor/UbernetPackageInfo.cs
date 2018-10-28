using System;

namespace Ubernet.Installer
{
    [Serializable]
    public class UbernetPackageInfo
    {
        public string name;
        public string displayName;
        public string description;
        public string version;
        public string author;
        public bool required;
        public string[] dependencies;
        
        public bool enabled;
        [NonSerialized] public string assetFullPath;
        [NonSerialized] public InstallationState state;

        public override string ToString()
        {
            return name;
        }

        public enum InstallationState
        {
            NotInstalled,
            Installing,
            Installed
        }
    }
}