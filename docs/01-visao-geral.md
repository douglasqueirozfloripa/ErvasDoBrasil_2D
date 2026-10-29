# Visão geral

**Ervas do Brasil** é um RPG cozy em 2D, com câmera de cima (top-down), que celebra as **plantas medicinais e espirituais do Brasil** e o **folclore popular**. Cada planta resolve um problema da personagem ou dos aldeões. O **Livro** (Grimório Botânico da Vó) ensina o uso tradicional, a cultura popular e os cuidados de cada erva.

## Pilares
1. **Aprender brincando:** toda planta tem uma página com benefícios tradicionais, cultura popular e cuidados reais.
2. **Cuidar:** de si (a fissura do cigarro, a ansiedade), da vila (aldeões doentes) e da horta.
3. **Proteger sem violência:** as assombrações são afastadas com plantas, amuletos, banhos e defumação. Ninguém morre.
4. **Orgulho da cultura brasileira:** raizeiras, benzedeiras, feira, chimarrão, tacacá, Saci, visagens, olho-gordo.

## Decisões fechadas
| Tema | Decisão |
|---|---|
| Câmera | Top-down (estilo Stardew Valley / Pokémon) |
| Combate | Só passivo |
| Plataformas | iPhone (Simulador iOS primeiro) e macOS |
| Engine | Unity 6.3 LTS, C# |
| Arquitetura | Regras em C# puro (`ErvasDoBrasil.Core`), testadas sem a Unity |
| Dados | JSON (`Assets/Resources/Data`), validado por testes |
| Estilo visual | "Quintal Minimalista": formas chapadas e arredondadas, contorno ameixa, paleta terrosa (ver [12-arte.md](12-arte.md)) |

## O que já funciona no protótipo v0.1
- Mundo jogável gerado em código: casa da vó (cama, caldeirão, varal, pilão), horta 4×3, feira, 3 aldeões e a mata com 16 ervas selvagens
- A jogadora com animação de 4 direções e poses de colher e pilar
- Ciclo dia/noite de 4 minutos, com sono, males ao amanhecer e noite em claro
- Os 3 status (energia, sanidade, aura) e 15 males com penalidades reais
- Horta (preparar, plantar, regar, colher) com 12 canteiros e a **pá**, que cava canteiros em qualquer canto do sítio para as plantas de proteção; varal, pilão, caldeirão com 28 receitas (20 preparos simples + 8 compostas), feira e aldeões
- **Nível de raizeira** (1 a 5): a experiência vem de colher, coletar, descobrir plantas, preparar e curar, e cada nível libera plantas na feira e na mata
- **Menu inicial e jogo salvo**: continuar, novo jogo, lista de jogos salvos; salva pelo menu (Esc) ou ao dormir
- **Histórico (H)**: os últimos avisos da partida (subir de nível e plantas liberadas em destaque), com selo de não lidos, que volta junto com o jogo salvo
- 4 assombrações com IA simples e proteção por auras, amuleto, arruda e defumação
- Livro com as abas Plantas, Males, Receitas e Assombrações
- Controles de toque (joystick + botões) no iPhone e teclado/mouse no Mac
- Arte 100% original no estilo "Quintal Minimalista" e trilha original por ambiente (dia, feira, noite)
- 211 testes unitários e 36 de interface, **lint** do C# (`./scripts/lint.sh`: analisadores .NET + Unity e `dotnet format`) e scripts de setup, build e testes
- Janela **Bastidores do desenvolvimento**: como o jogo foi feito, ciclo de vida do app, Unity × Web e um diário ao vivo do ciclo de vida

## O que falta (ver [13-escopo-freelancer.md](13-escopo-freelancer.md))
Tilemap e colisões, UI final, efeitos sonoros, diálogos e cutscenes do [roteiro](02-enredo-e-roteiro.md).
