# Arte: estilo "Quintal Minimalista"

Toda a arte do jogo é **original** e gerada em código por `tools/arte/`. Nenhuma imagem de terceiros ou de referência entra no jogo, e todas as peças seguem o mesmo conceito visual.

## O conceito
| Regra | Como aparece |
|---|---|
| **Formas chapadas e arredondadas** | sem texturas nem degradês; volume só com um tom mais escuro |
| **Contorno ameixa suave** `#3A2834` | o mesmo em tudo: plantas, gente, espíritos, ícones e mapa |
| **Paleta terrosa de quintal** | terra `#80583A`, telha `#C4623E`, caiado `#F4ECDA`, avental `#628C4E`, azul de janela `#4870A0` |
| **Personagens chibi** | cabeça grande, olhos de pontinho, bochecha rosada |
| **Plantas no montinho de terra** | todos os 5 estágios saem do mesmo montinho, então o crescimento é legível |
| **Espíritos com brilho** | as únicas peças com luz própria (lamparina, olhos da visagem, aura verde do olho-gordo) |

## Regenerar ou ajustar
```bash
pip3 install pillow
python3 tools/arte/gerar_arte.py --preview    # regenera Assets/Art e salva tools/arte/preview.png
python3 tools/arte/icone.py                   # só o ícone do app (não mexe em Assets/Art)
```
| Módulo | O que ajustar |
|---|---|
| `estilo.py` | paleta, espessura do contorno, primitivas (folha, montinho, sombra) |
| `plantas.py` | `SPECS`: hábito (arbusto, árvore, trepadeira, touceira, roseta...), forma da folha, cor e flor de cada planta |
| `personagens.py` | `RAIZEIRA` e `ALDEOES` (pele, roupa, chapéu, barba, terço...) |
| `espiritos.py` | as 4 assombrações |
| `cenario.py` | casa, caldeirão, varal, pilão, feira, ícones dos preparos e mapa |
| `icone.py` | ícone do app (1024 × 1024): a Vó benzedeira (`VO` em `personagens.py`) com terço e ramo de arruda. Salva `Assets/Icon/app_icon.png` (iOS: quadrado opaco, o sistema recorta), `Assets/Icon/app_icon_macos.png` (Mac: squircle de 824 px com margem transparente e sombra, na grade de ícones da Apple) e `tools/arte/preview_icone.png`. Fica fora de `Assets/Art` para não virar Sprite. O `BuildTools.Configure` usa o primeiro como padrão e o segundo no Mac |

Ao abrir a Unity, tudo em `Assets/Art` vira Sprite e entra sozinho na **ArtLibrary**.

## O que existe
| Arquivo | Quantidade |
|---|---|
| `Plants/plant_<id>_0..4` | 23 plantas × 5 estágios (semente, broto, jovem, crescendo, madura) |
| `Characters/player_{down,up,right}_0..2` | raizeira andando (a esquerda é a direita espelhada) |
| `Characters/player_idle`, `player_forage`, `player_grind`, `player_portrait` | parada com ramo de arruda, colhendo, pilando, retrato |
| `Characters/npc_{ze,cida,tonhao,maria,juca,lucia}` | um por aldeão, com o id de `Village.Ids`: Seu Zé (chapéu de palha e barba), Dona Cida (lenço e avental), Tonhão da Venda (cabelo curto e bigode), Benzedeira Maria (coque branco e terço), Pescador Juca (boné), Professora Lúcia (coque castanho e óculos). A arte segue o nome do pedido, não o lugar na vila |
| `Characters/spirit_{alma,visagem,saci,olhogordo,cuca,bernuncia}_0..1` | assombrações e lendas (2 quadros) |
| `Environment/prop_{casa,caldeirao,varal,pilao,feira}` | estações |
| `Environment/map_fazenda` | mapa 36 × 24 unidades (48 px/unidade), alinhado com as posições do `GameManager`. A horta é só um gramado cercado: os canteiros (terra com contorno, sulcos quando preparados) são desenhados em código pelo `SpriteFactory.Bed`, iguais aos que a pá cava pelo mapa |
| `UI/Icons/icon_<receita>` | 28 ícones: xícara (chá), garrafa (garrafada), frasco brilhante (elixir), vidro de xarope, bacia (banho), patuá (amuleto), defumador, pote (gel), cuia (chimarrão), ramo |
| `UI/Icons/icon_pa` | a pá de cavar canteiros (feira e bolsa) |

## Convenções para arte nova (feita à mão ou por outro artista)
- PNG com fundo transparente. Personagens, plantas e props ficam com pivô no pé (configurado automaticamente).
- O sufixo `_px` força o filtro *Point* (pixel art).
- Arquivos começando com `_` são ignorados pelo jogo.
- Para substituir uma peça gerada, salve com o **mesmo nome**. Se depois rodar o gerador, ele sobrescreve, então ajuste o `SPECS` ou tire a peça do gerador.
