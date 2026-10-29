# Trilha sonora

## Como funciona
O `MusicManager` toca uma faixa por ambiente e faz **crossfade de 2,5 s** quando o ambiente muda. A regra de escolha fica em `Core/MusicRules.cs`, coberta por testes:

| Faixa | Arquivo | Quando toca |
|---|---|---|
| Dia | `trilha_dia` | de dia, na horta, na casa e na mata |
| Feira | `trilha_feira` | de dia, a até 7 m da vila/feira |
| Noite | `trilha_noite` | à noite, ou quando a raizeira está "Assombrada" |

Os arquivos ficam em `Assets/Audio/Resources/Music/` e são importados em *streaming* (Vorbis), para economizar memória no iPhone.

## Faixas atuais: originais
Compostas e sintetizadas em código por `tools/musica/compor.py` (sem samples nem trechos de outras músicas). São loops perfeitos de 16 compassos:

| Faixa | Título | Clima |
|---|---|---|
| `trilha_dia` | *Quintal ao Sol* | groove inspirado em ijexá, 100 bpm, agogô, xequerê, baixo e kalimba em Ré pentatônica |
| `trilha_feira` | *Feira de Raiz Funda* | groove inspirado em baião, 118 bpm, zabumba, triângulo e sanfona em Lá mixolídio |
| `trilha_noite` | *Lamparina na Neblina* | groove inspirado em reggae one-drop, 72 bpm, skank de órgão, baixo dub e melódica com eco em Mi dórico |

```bash
pip3 install numpy          # e ffmpeg (brew install ffmpeg) para gerar .ogg
python3 tools/musica/compor.py
```

## Sobre as músicas de referência
As músicas pedidas como referência de clima foram "Banho de Folhas" (Luedji Luna), "A Feira" (O Rappa) e "Santa Kaya" (Ponto de Equilíbrio). Elas são protegidas por direito autoral, então não foram reproduzidas, transcritas nem imitadas, nem mesmo como instrumental. Para usar as faixas originais (ou o instrumental delas) no jogo é preciso uma **licença de sincronização** com a editora (composição) e uma **licença de master** com a gravadora (gravação), ou encomendar regravações autorizadas.

Com a licença em mãos, basta substituir os arquivos **mantendo os nomes** (`.ogg`, `.mp3` ou `.wav`). Sugestão de encaixe:

| Faixa do jogo | Referência de clima |
|---|---|
| `trilha_dia` | Banho de Folhas |
| `trilha_feira` | A Feira |
| `trilha_noite` | Santa Kaya |

Outra saída é encomendar a um músico faixas originais com esse clima, ou usar as do `compor.py` como base.

## Volume
São duas camadas:

- **Mixagem da trilha:** `MusicManager.Volume` (padrão 0,55) é o nível da música dentro do jogo, usado pelo crossfade.
- **Volume do jogo e mudo:** o controle no rodapé, à direita (botão do alto-falante, slider e porcentagem; tecla **M** para o mudo). A regra fica em `Core/SoundSettings.cs` (volume de 0 a 1, padrão 0,8; o mudo guarda o volume para voltar a ele; mexer no slider tira do mudo), coberta por `SoundSettingsTests`. `World/GameAudio.cs` passa o valor para o `AudioListener.volume`, o volume geral da Unity, que vale para a música e para os efeitos (o som de subir de nível) e não mexe no volume do sistema. A escolha fica no `PlayerPrefs` (`som.volume`, `som.mudo`) e vale para todas as partidas; os testes de interface trocam o começo das chaves (`GameAudio.Chave = "ui-tests.som"`) para não mexer no volume do jogo de verdade, e `SomTests` cobre a barra, o mudo, a tecla M e a escolha voltando ao abrir o jogo. O desenho do controle está em `UI/SoundControl.cs`.

Volumes separados para música e efeitos ainda estão no escopo (ver [13-escopo-freelancer.md](13-escopo-freelancer.md)).
