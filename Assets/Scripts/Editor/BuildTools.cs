using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace ErvasDoBrasil.EditorTools
{
    /// <summary>
    /// Cena principal, Player Settings e builds para iOS Simulator e macOS.
    /// Use pelo menu "Ervas do Brasil" ou pela linha de comando (scripts/*.sh chamam os métodos CI_*).
    /// </summary>
    [InitializeOnLoad]
    public static class BuildTools
    {
        public const string ScenePath = "Assets/Scenes/Main.unity";
        public const string BundleId = "com.douglasqueiroz.ervasdobrasil";
        public const string ProductName = "Ervas do Brasil";
        public const string IosSimulatorDir = "Builds/iOS-Simulator";
        public const string MacAppPath = "Builds/macOS/ErvasDoBrasil.app";
        public const string IconPath = "Assets/Icon/app_icon.png";
        public const string MacIconPath = "Assets/Icon/app_icon_macos.png";

        static BuildTools()
        {
            EditorApplication.delayCall += EnsureLegacyInput;
        }

        // ───────── Cena ─────────

        [MenuItem("Ervas do Brasil/Criar ou abrir cena principal", priority = 0)]
        public static void OpenMainScene()
        {
            EnsureScene();
            EditorSceneManager.OpenScene(ScenePath);
        }

        public static void EnsureScene()
        {
            if (!File.Exists(ScenePath))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(ScenePath));
                var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
                var light = GameObject.Find("Directional Light");
                if (light != null) UnityEngine.Object.DestroyImmediate(light);
                var cam = Camera.main;
                if (cam != null)
                {
                    cam.orthographic = true;
                    cam.orthographicSize = 5.5f;
                    cam.transform.position = new Vector3(0, 0, -10);
                }
                new GameObject("GameManager").AddComponent<GameManager>();
                EditorSceneManager.SaveScene(scene, ScenePath);
                Debug.Log("[Ervas do Brasil] Cena criada em " + ScenePath);
            }
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
        }

        // ───────── Player Settings ─────────

        [MenuItem("Ervas do Brasil/Configurar Player Settings (iOS Simulator + macOS)", priority = 20)]
        public static void ConfigureMenu() { Configure(true); }

        public static void Configure(bool iosSimulator)
        {
            PlayerSettings.companyName = "Douglas Queiroz";
            PlayerSettings.productName = ProductName;
            PlayerSettings.bundleVersion = "0.1.0";
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.iOS, BundleId);
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Standalone, BundleId);

            // jogo em paisagem
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.AutoRotation;
            PlayerSettings.allowedAutorotateToPortrait = false;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = true;
            PlayerSettings.allowedAutorotateToLandscapeRight = true;

            // iOS
            PlayerSettings.iOS.targetOSVersionString = "15.0";
            PlayerSettings.iOS.sdkVersion = iosSimulator ? iOSSdkVersion.SimulatorSDK : iOSSdkVersion.DeviceSDK;
            TrySetSimulatorArchitecture();

            // macOS (Mono para builds rápidos de desenvolvimento)
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Standalone, ScriptingImplementation.Mono2x);
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
            // sem foco o app continua rodando: a música não corta ao trocar de janela (o jogo abre o Menu e pausa sozinho)
            PlayerSettings.runInBackground = true;
            PlayerSettings.defaultScreenWidth = 1280;
            PlayerSettings.defaultScreenHeight = 720;
            PlayerSettings.resizableWindow = true;

            // ícone do app: a Vó benzedeira (gerado por tools/arte/icone.py)
            SetAppIcon();

            // sem HTTP aberto, sem relatório de crash e sem estatísticas de hardware (docs/15-seguranca.md)
            SecurityGate.HardenPlayerSettings();

            EnsureLegacyInput();
            AssetDatabase.SaveAssets();
        }

        /// <summary>
        /// Ícones do app (gerados por tools/arte/icone.py), importados sem compressão:
        /// app_icon.png é o padrão (iOS, quadrado opaco que o sistema recorta) e app_icon_macos.png vale para o Mac,
        /// que não recorta: o squircle com margem e sombra já vem desenhado, como nos outros ícones do Dock.
        /// </summary>
        static void SetAppIcon()
        {
            var icon = LoadIcon(IconPath);
            if (icon == null)
            {
                Debug.LogWarning("[Ervas do Brasil] Ícone não encontrado em " + IconPath + " (gere com: python3 tools/arte/icone.py)");
                return;
            }
            PlayerSettings.SetIcons(NamedBuildTarget.Unknown, new[] { icon }, IconKind.Any);

            var mac = LoadIcon(MacIconPath);
            if (mac == null)
            {
                Debug.LogWarning("[Ervas do Brasil] Ícone do macOS não encontrado em " + MacIconPath + "; o Mac usa o quadrado.");
                return;
            }
            // o Standalone pede um ícone por tamanho (1024, 512... 16); a Unity reduz o mesmo PNG para cada um
            int sizes = PlayerSettings.GetIconSizes(NamedBuildTarget.Standalone, IconKind.Any).Length;
            var macIcons = new Texture2D[sizes];
            for (int i = 0; i < sizes; i++) macIcons[i] = mac;
            PlayerSettings.SetIcons(NamedBuildTarget.Standalone, macIcons, IconKind.Any);
        }

        static Texture2D LoadIcon(string path)
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer != null && (importer.textureCompression != TextureImporterCompression.Uncompressed ||
                                     importer.mipmapEnabled || importer.maxTextureSize != 1024 || !importer.alphaIsTransparency))
            {
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.mipmapEnabled = false;
                importer.maxTextureSize = 1024;
                importer.alphaIsTransparency = true;
                importer.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        /// <summary>Simulador no Mac Apple Silicon precisa de arm64 (Universal cobre Intel também).</summary>
        static void TrySetSimulatorArchitecture()
        {
            try
            {
                var prop = typeof(PlayerSettings.iOS).GetProperty("simulatorSdkArchitecture", BindingFlags.Public | BindingFlags.Static);
                if (prop == null || !prop.CanWrite || !prop.PropertyType.IsEnum) return;
                foreach (var name in new[] { "Universal", "ARM64", "Arm64" })
                {
                    if (Enum.IsDefined(prop.PropertyType, name))
                    {
                        prop.SetValue(null, Enum.Parse(prop.PropertyType, name));
                        return;
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning("Não consegui definir a arquitetura do simulador: " + e.Message);
            }
        }

        /// <summary>O protótipo usa o Input Manager clássico; se estiver só no Input System, muda para "Both".</summary>
        static void EnsureLegacyInput()
        {
            var assets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset");
            if (assets == null || assets.Length == 0) return;
            var so = new SerializedObject(assets[0]);
            var p = so.FindProperty("activeInputHandler");
            if (p != null && p.intValue == 1)
            {
                p.intValue = 2;
                so.ApplyModifiedPropertiesWithoutUndo();
                Debug.LogWarning("[Ervas do Brasil] Active Input Handling ajustado para 'Both'. Reinicie a Unity para aplicar.");
            }
        }

        // ───────── Builds ─────────

        [MenuItem("Ervas do Brasil/Build ▸ iOS Simulator (projeto Xcode)", priority = 40)]
        public static void BuildIosSimulatorMenu() { BuildIosSimulator(); }

        [MenuItem("Ervas do Brasil/Build ▸ macOS (.app)", priority = 41)]
        public static void BuildMacMenu() { BuildMac(); }

        public static bool BuildIosSimulator()
        {
            EnsureScene();
            Configure(true);
            ArtLibraryBuilder.Rebuild();
            EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.iOS, BuildTarget.iOS);
            return Build(BuildTarget.iOS, IosSimulatorDir);
        }

        public static bool BuildMac()
        {
            EnsureScene();
            Configure(false);
            ArtLibraryBuilder.Rebuild();
            EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Standalone, BuildTarget.StandaloneOSX);
            return Build(BuildTarget.StandaloneOSX, MacAppPath);
        }

        /// <summary>
        /// Release por padrão. O build de desenvolvimento (Profiler, console de erros na tela) escuta na porta 55000 e
        /// anuncia o IP e o nome do computador na rede local, então só sai com -devBuild (./scripts/build-macos.sh --dev).
        /// </summary>
        static bool Build(BuildTarget target, string path)
        {
            bool dev = Array.IndexOf(Environment.GetCommandLineArgs(), "-devBuild") >= 0;
            var options = new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                locationPathName = path,
                target = target,
                options = dev ? BuildOptions.Development : BuildOptions.None
            };
            BuildReport report;
            SecurityGate.AllowDevelopmentBuild = dev;
            try { report = BuildPipeline.BuildPlayer(options); }
            finally { SecurityGate.AllowDevelopmentBuild = false; }
            var ok = report.summary.result == BuildResult.Succeeded;
            Debug.Log("[Ervas do Brasil] Build " + target + (dev ? " (desenvolvimento)" : " (release)") + ": " + report.summary.result +
                      " → " + path + " (" + report.summary.totalErrors + " erros, " + report.summary.totalWarnings + " avisos)");
            if (ok && !dev) ok = SecurityGate.VerifyPlayer(target, path);
            return ok;
        }

        // ───────── Linha de comando ─────────

        public static void CI_BuildIosSimulator() { EditorApplication.Exit(BuildIosSimulator() ? 0 : 1); }
        public static void CI_BuildMac() { EditorApplication.Exit(BuildMac() ? 0 : 1); }

        public static void CI_Prepare()
        {
            EnsureScene();
            ArtLibraryBuilder.Rebuild();
            Configure(true);
            EditorApplication.Exit(0);
        }
    }
}
