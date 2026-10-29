using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace ErvasDoBrasil.Core
{
    /// <summary>
    /// O jogo é offline: não faz requisições, não abre portas e não manda dados para ninguém.
    /// Estas regras guardam isso: o SecurityGate (Editor) as aplica em todo build e o SecurityTests as confere.
    /// Ver docs/15-seguranca.md.
    /// </summary>
    public static class SecurityRules
    {
        /// <summary>Prefixos de pacotes que trazem rede ou telemetria. Nenhum pode entrar no projeto, nem como dependência.</summary>
        public static readonly string[] NetworkPackages =
        {
            "com.unity.modules.unitywebrequest", // UnityWebRequest e os módulos .assetbundle, .audio, .texture e .www
            "com.unity.modules.unityanalytics",
            "com.unity.analytics",
            "com.unity.services.",               // Unity Gaming Services: analytics, cloud save, remote config, lobby...
            "com.unity.remote-config",
            "com.unity.purchasing",
            "com.unity.ads",
            "com.unity.netcode",
            "com.unity.transport",
            "com.unity.multiplayer",
            "com.google.firebase",
            "com.google.ads",
            "com.facebook.",
        };

        static readonly Regex PackageKey = new Regex("\"([a-z0-9][a-z0-9._-]*)\"\\s*:", RegexOptions.CultureInvariant);

        /// <summary>Pacotes de rede citados num Packages/manifest.json ou packages-lock.json (o lock lista também as dependências).</summary>
        public static List<string> ForbiddenPackages(string packagesJson)
        {
            var found = new List<string>();
            if (string.IsNullOrEmpty(packagesJson)) return found;
            foreach (Match m in PackageKey.Matches(packagesJson))
            {
                string name = m.Groups[1].Value;
                if (found.Contains(name)) continue;
                foreach (var prefix in NetworkPackages)
                {
                    if (name.StartsWith(prefix, StringComparison.Ordinal))
                    {
                        found.Add(name);
                        break;
                    }
                }
            }
            return found;
        }

        /// <summary>
        /// O app gerado liga a conexão do Profiler? No build de desenvolvimento o boot.config traz
        /// player-connection-mode=Listen e o IP da máquina: o jogo escuta na porta 55000 e anuncia IP e nome do computador na rede local.
        /// </summary>
        public static bool OpensPlayerConnection(string bootConfig)
        {
            if (string.IsNullOrEmpty(bootConfig)) return false;
            foreach (var line in bootConfig.Split('\n'))
                if (line.TrimStart().StartsWith("player-connection", StringComparison.Ordinal)) return true;
            return false;
        }
    }
}
