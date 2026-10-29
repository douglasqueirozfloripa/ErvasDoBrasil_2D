using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using ErvasDoBrasil.Core;
using UnityEngine;

namespace ErvasDoBrasil
{
    /// <summary>
    /// Jogos salvos em JSON, um arquivo por partida, em Application.persistentDataPath/saves
    /// (no Mac: ~/Library/Application Support/Douglas Queiroz/Ervas do Brasil/saves).
    /// </summary>
    public static class SaveStore
    {
        const string DateFormat = "yyyy-MM-dd HH:mm:ss";

        static string dirOverride;

        /// <summary>Pasta dos saves. Os testes de interface apontam para outra pasta (null volta ao padrão) para não tocar nos saves de verdade.</summary>
        public static string Dir
        {
            get { return dirOverride ?? Path.Combine(Application.persistentDataPath, "saves"); }
            set { dirOverride = value; }
        }

        static string PathOf(string id) { return Path.Combine(Dir, id + ".json"); }

        public static string NewId() { return "partida_" + DateTime.Now.ToString("yyyyMMdd_HHmmss", CultureInfo.InvariantCulture); }

        /// <summary>Data no formato gravado (ordena como texto).</summary>
        public static string Now() { return DateTime.Now.ToString(DateFormat, CultureInfo.InvariantCulture); }

        /// <summary>Data gravada → "15/10/2026 14:32" (ou "15/10/2026 14:32:05", para separar saves do mesmo minuto).</summary>
        public static string Pretty(string stamp, bool seconds = false)
        {
            DateTime t;
            return DateTime.TryParseExact(stamp, DateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out t)
                ? t.ToString(seconds ? "dd/MM/yyyy HH:mm:ss" : "dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture)
                : "data desconhecida";
        }

        /// <summary>
        /// Partidas salvas, a mais recente primeiro. Arquivos ilegíveis, com nome inválido ou maiores que
        /// SaveGame.MaxBytes são ignorados: o save é entrada não confiável (docs/15-seguranca.md).
        /// </summary>
        public static List<SaveData> List()
        {
            var list = new List<SaveData>();
            if (!Directory.Exists(Dir)) return list;
            foreach (var file in Directory.GetFiles(Dir, "*.json"))
            {
                string id = Path.GetFileNameWithoutExtension(file);
                try
                {
                    if (!SaveGame.IdValido(id)) continue;
                    if (new FileInfo(file).Length > SaveGame.MaxBytes)
                    {
                        Debug.LogWarning("[Save] Ignorando " + id + ".json: maior que " + SaveGame.MaxBytes / 1024 + " KB");
                        continue;
                    }
                    var d = JsonUtility.FromJson<SaveData>(File.ReadAllText(file));
                    if (d == null) continue;
                    d.id = id; // o nome do arquivo manda
                    list.Add(d);
                }
                catch (Exception e)
                {
                    Debug.LogWarning("[Save] Ignorando " + id + ".json: " + Privacy.Redact(e.Message));
                }
            }
            return list.OrderByDescending(d => d.salvoEm ?? "", StringComparer.Ordinal).ToList();
        }

        /// <summary>Grava primeiro num temporário: se a conversão falhar, o save anterior fica intacto.</summary>
        public static bool Write(SaveData d)
        {
            if (d == null || !SaveGame.IdValido(d.id)) return false;
            try
            {
                Directory.CreateDirectory(Dir);
                string path = PathOf(d.id);
                string tmp = path + ".tmp";
                File.WriteAllText(tmp, JsonUtility.ToJson(d, true));
                File.Copy(tmp, path, true);
                File.Delete(tmp);
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError("[Save] Falha ao salvar " + d.id + ": " + Privacy.Redact(e.ToString()));
                return false;
            }
        }

        const int LixeiraGuarda = 20;

        /// <summary>Jogos apagados ficam aqui (fora da lista) para o "Desfazer"; guarda os últimos 20.</summary>
        static string Lixeira { get { return Path.Combine(Dir, "lixeira"); } }

        /// <summary>Apaga um jogo salvo da lista. O arquivo vai para saves/lixeira e <see cref="Undelete"/> o traz de volta.</summary>
        public static bool Delete(string id)
        {
            try
            {
                if (!SaveGame.IdValido(id)) return false;
                string src = PathOf(id);
                if (!File.Exists(src)) return false;
                Directory.CreateDirectory(Lixeira);
                string dst = Path.Combine(Lixeira, id + ".json");
                if (File.Exists(dst)) File.Delete(dst);
                File.Move(src, dst);
                File.SetLastWriteTimeUtc(dst, DateTime.UtcNow); // a lixeira descarta pela data em que foi apagado
                foreach (var old in new DirectoryInfo(Lixeira).GetFiles("*.json").OrderByDescending(f => f.LastWriteTimeUtc).Skip(LixeiraGuarda))
                    old.Delete();
                return true;
            }
            catch (Exception e)
            {
                Debug.LogWarning("[Save] Falha ao apagar " + id + ": " + Privacy.Redact(e.Message));
                return false;
            }
        }

        /// <summary>Traz de volta da lixeira um jogo apagado (se não houver outro com o mesmo nome na lista).</summary>
        public static bool Undelete(string id)
        {
            try
            {
                if (!SaveGame.IdValido(id)) return false;
                string src = Path.Combine(Lixeira, id + ".json");
                if (!File.Exists(src) || File.Exists(PathOf(id))) return false;
                File.Move(src, PathOf(id));
                return true;
            }
            catch (Exception e)
            {
                Debug.LogWarning("[Save] Falha ao restaurar " + id + ": " + Privacy.Redact(e.Message));
                return false;
            }
        }
    }
}
