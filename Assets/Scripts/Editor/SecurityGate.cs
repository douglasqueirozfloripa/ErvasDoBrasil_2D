using System.Collections.Generic;
using System.IO;
using ErvasDoBrasil.Core;
using UnityEditor;
using UnityEditor.Advertisements;
using UnityEditor.Analytics;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.CrashReporting;
using UnityEditor.Purchasing;
using UnityEngine;

namespace ErvasDoBrasil.EditorTools
{
    /// <summary>
    /// Portão de segurança: roda antes de todo build (scripts, menu ou Build Settings) e reprova o que abriria o jogo
    /// para a rede. O jogo é offline; as listas ficam em Core/SecurityRules.cs. Ver docs/15-seguranca.md.
    /// </summary>
    public class SecurityGate : IPreprocessBuildWithReport
    {
        static readonly string[] PackageFiles = { "Packages/manifest.json", "Packages/packages-lock.json" };

        public int callbackOrder { get { return 0; } }

        /// <summary>Build de desenvolvimento pedido de propósito (./scripts/build-macos.sh --dev).</summary>
        public static bool AllowDevelopmentBuild { get; set; }

        public void OnPreprocessBuild(BuildReport report)
        {
            var problems = Check(report.summary.options);
            if (problems.Count > 0)
                throw new BuildFailedException("[Segurança] Build bloqueado (docs/15-seguranca.md):\n- " + string.Join("\n- ", problems));
            Debug.Log("[Segurança] Portão OK: sem pacotes de rede, sem HTTP, sem serviços da Unity.");
        }

        public static List<string> Check(BuildOptions options)
        {
            var problems = new List<string>();
            foreach (var file in PackageFiles)
            {
                if (!File.Exists(file)) continue;
                foreach (var p in SecurityRules.ForbiddenPackages(File.ReadAllText(file)))
                    problems.Add("pacote de rede " + p + " em " + file);
            }
            if (PlayerSettings.insecureHttpOption != InsecureHttpOption.NotAllowed)
                problems.Add("o Player Settings permite HTTP sem criptografia (Allow downloads over HTTP)");
            if (AnalyticsSettings.enabled) problems.Add("Unity Analytics ligado");
            if (PerformanceReportingSettings.enabled) problems.Add("Performance Reporting ligado");
            if (CrashReportingSettings.enabled) problems.Add("Cloud Diagnostics (relatório de crash) ligado");
            if (AdvertisementSettings.enabled) problems.Add("Unity Ads ligado");
            if (PurchasingSettings.enabled) problems.Add("Unity IAP ligado");

            // os players dos testes de interface precisam da conexão para devolver os resultados
            bool testPlayer = (options & BuildOptions.IncludeTestAssemblies) != 0;
            if ((options & BuildOptions.Development) != 0 && !testPlayer && !AllowDevelopmentBuild)
                problems.Add("build de desenvolvimento: o jogo escutaria na porta 55000 e anunciaria IP e nome do computador " +
                             "na rede local. Para gerar mesmo assim: ./scripts/build-macos.sh --dev");
            return problems;
        }

        /// <summary>
        /// Desliga o que mandaria dados para fora. Chamado pelo BuildTools.Configure em todo build.
        /// As estatísticas de hardware ("Disable HW Statistics") não têm propriedade pública; vão pelo ProjectSettings.asset.
        /// </summary>
        public static void HardenPlayerSettings()
        {
            PlayerSettings.insecureHttpOption = InsecureHttpOption.NotAllowed;
            PlayerSettings.enableCrashReportAPI = false;

            var assets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset");
            if (assets == null || assets.Length == 0) return;
            var so = new SerializedObject(assets[0]);
            var hw = so.FindProperty("submitAnalytics");
            if (hw != null && hw.boolValue)
            {
                hw.boolValue = false;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        /// <summary>Depois de um build release: o boot.config do app não pode ligar a conexão do Profiler.</summary>
        public static bool VerifyPlayer(BuildTarget target, string path)
        {
            string boot = target == BuildTarget.StandaloneOSX ? Path.Combine(path, "Contents/Resources/Data/boot.config")
                        : target == BuildTarget.iOS ? Path.Combine(path, "Data/boot.config")
                        : null;
            if (boot == null || !File.Exists(boot))
            {
                Debug.LogWarning("[Segurança] boot.config não encontrado em " + boot + "; o app gerado não foi conferido.");
                return true;
            }
            if (SecurityRules.OpensPlayerConnection(File.ReadAllText(boot)))
            {
                Debug.LogError("[Segurança] O app gerado liga a conexão do Profiler na rede (" + boot + "). Build reprovado.");
                return false;
            }
            Debug.Log("[Segurança] App conferido: não abre porta na rede.");
            return true;
        }
    }
}
