# Plantas e receitas

Dados em `Assets/Resources/Data/plants.json` e `recipes.json`. Cada planta com `remedio` gera sozinha um **preparo simples** (chá, gel, ramo, banho ou defumação) no Caldeirão. 🇧🇷 = nativa do Brasil.

## As 23 plantas do Livro

| Planta | Nome científico | Tipo | No jogo | Onde achar |
|---|---|---|---|---|
| **Boldo**  | *Plectranthus barbatus* | Medicinal | Cura dor de barriga e náusea. Recupera 30% da energia física. | feira |
| **Camomila**  | *Matricaria chamomilla* | Medicinal | Reduz a ansiedade e restaura 35 de sanidade. Diminui o tremor da tela. | feira |
| **Mulungu** 🇧🇷 | *Erythrina mulungu / E. velutina* | Medicinal | Cura a Fissura (vontade de fumar) em 3 doses e acalma a ansiedade. Recupera 15 de sanidade. | mata + feira |
| **Espada-de-São-Jorge**  | *Dracaena trifasciata (antiga Sansevieria)* | Espiritual | Plantada e madura, cria uma barreira de 3 m que nenhum espírito atravessa. | feira |
| **Arruda**  | *Ruta graveolens* | Espiritual | Limpa o mau-olhado (uruca), +40 de aura e revela visagens ocultas por 60 s. | feira |
| **Guaraná** 🇧🇷 | *Paullinia cupana* | Energética | Cura o cansaço e dá +50% de velocidade por 90 s. | mata + feira |
| **Quebra-pedra** 🇧🇷 | *Phyllanthus niruri* | Medicinal | Cura a dor nos rins e remove a lerdeza severa. | mata + feira |
| **Erva-cidreira** 🇧🇷 | *Lippia alba* | Medicinal | Cura a insônia e recupera 15 de sanidade. | feira |
| **Maracujá (folha)** 🇧🇷 | *Passiflora edulis* | Medicinal | Acalma a ansiedade e a insônia. Recupera 20 de sanidade. | mata + feira |
| **Guaco** 🇧🇷 | *Mikania glomerata* | Medicinal | Cura a tosse (que atrai espíritos de longe). | mata + feira |
| **Hortelã**  | *Mentha spp.* | Medicinal | Cura a náusea. | feira |
| **Espinheira-santa** 🇧🇷 | *Maytenus ilicifolia* | Medicinal | Cura a azia (ações voltam a custar energia normal). | mata + feira |
| **Gengibre**  | *Zingiber officinale* | Medicinal | Cura a gripe (energia máxima volta a 100). | feira |
| **Babosa**  | *Aloe vera* | Medicinal | Cura queimadura (libera o caldeirão de novo). | feira |
| **Jambu** 🇧🇷 | *Acmella oleracea* | Medicinal | Cura a dor de dente (preços da feira voltam ao normal). | mata + feira |
| **Capim-santo**  | *Cymbopogon citratus* | Medicinal | Cura a cólica. Recupera 10 de sanidade. | feira |
| **Carqueja** 🇧🇷 | *Baccharis trimera* | Medicinal | Cura dor de barriga. | mata + feira |
| **Erva-mate** 🇧🇷 | *Ilex paraguariensis* | Energética | Cura o cansaço e recupera 40% da energia. | feira |
| **Guiné** 🇧🇷 | *Petiveria alliacea* | Espiritual | Defumação bane espíritos num raio de 5 m e cura 'assombrado'. Plantada, afasta o Saci. | mata + feira |
| **Alecrim**  | *Salvia rosmarinus* | Espiritual | Banho de alecrim dá sorte (vende +30%, colhe +1) e recupera sanidade e aura. | feira |
| **Pimenteira** 🇧🇷 | *Capsicum frutescens* | Espiritual | Plantada e madura, cria uma aura de 4 m que o Olho-Gordo não atravessa. | feira |
| **Comigo-ninguém-pode**  | *Dieffenbachia seguine* | Espiritual | Plantada e madura, cria uma barreira de 3 m contra todos os espíritos. | feira |
| **Manjericão**  | *Ocimum basilicum* | Espiritual | Banho de manjericão dá sorte (vende +30%, colhe +1) e acalma. | feira |

Cada página do Livro mostra: **No jogo · Benefícios tradicionais · Sobre a planta · Como preparar · Cultura popular · Origem e bioma · Cuidados**.

## Nível para cultivar
Cada planta tem `"nivel"` no `plants.json`: é o nível de raizeira para comprar a semente, plantar e coletar na mata (tabela em [09-mecanicas.md](09-mecanicas.md#nível-de-raizeira)). As sementes do kit inicial precisam ser do nível 1, e um teste garante isso.

## Fluxo de preparo

```
Erva fresca ──Varal (45 s)──▶ Erva seca ──Pilão (3 energia)──▶ Pó
      └──────────────┬───────────────┘                         │
                     ▼                                         ▼
                Caldeirão (5 energia) ◀────────────────────────┘
```


## Receitas compostas
| Receita | Ingredientes | Efeito |
|---|---|---|
| Garrafada da Vó | boldo, carqueja, espinheira (secas) | cura barriga, náusea e azia; +30% energia |
| Elixir da Calmaria | 2 camomila (pó), maracujá (pó), cidreira | ansiedade + insônia; +60 sanidade |
| Elixir do Recomeço | 2 mulungu (pó), camomila | fissura (2 doses) + ansiedade |
| Xarope de Guaco | 2 guaco, gengibre, hortelã | tosse + gripe |
| Elixir da Mata | guaraná (pó), mate (seca), gengibre | energia total, +50% velocidade por 2 min |
| Banho de Descarrego | arruda, guiné, alecrim | limpa mau-olhado e assombração, aura cheia, sorte |
| Defumação Forte | 2 guiné (secas), alecrim (seco) | bane espíritos num raio de 8 m |
| Amuleto de Arruda e Pimenta | 2 arruda, pimenteira | amuleto por 2 dias, +30 aura |

Elixires e garrafadas têm 5–10% de chance de **queimar a mão** (a babosa cura).

## Plantas de proteção (aura quando maduras)
| Planta | Aura | Raio |
|---|---|---|
| Espada-de-São-Jorge | Barreira (todos) | 3 m |
| Comigo-ninguém-pode | Barreira (todos) | 3 m |
| Pimenteira | Anti-Olho-Gordo | 4 m |
| Guiné | Anti-Saci | 3,5 m |
