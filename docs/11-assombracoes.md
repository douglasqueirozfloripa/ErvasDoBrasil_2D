# Assombrações (combate passivo)

As entidades saem à noite e somem ao amanhecer. Com "Assombrado", elas aparecem até de dia. **Nas duas primeiras noites a casa ainda está benzida pela vó** (`ProtectionRules.NoitesSeguras = 2`) e nada aparece: dá tempo de plantar a primeira Espada-de-São-Jorge. Ao anoitecer, um aviso diz se a noite é tranquila ou se a bênção acabou. Ninguém luta: elas são **afastadas** (auras, amuleto) ou **banidas** (defumação). As fichas completas estão no Livro, aba **Assombrações**, e no código em `Core/SpiritLore.cs`.

## Alma da Lamparina
A lamparina é a luminária antiga e portátil, de óleo ou querosene, de luz amarelada, suave e trêmula.
- **Simboliza:** *resiliência*, uma alma que mantém acesa a pequena chama de esperança, fé ou sabedoria mesmo na escuridão. E também *fragilidade*, uma existência antiga e humilde que brilha oscilante como o pavio ao vento.
- **No jogo:** ao te atravessar, tira 20 de energia, 15 de aura e 10 de sanidade. Com a aura fraca, deixa você "Assombrada". Por perto, a sanidade cai devagar.
- **Proteção:** amuleto de arruda, barreira de Espada-de-São-Jorge ou Comigo-ninguém-pode, defumação de guiné, banho de descarrego.
- **Arte:** fantasminha lilás de olhos ocos segurando a lamparina (`spirit_alma_0..1`).

## Visagem da Neblina
No Norte e no Nordeste, *visagem* é aparição, fantasma, assombração. A neblina nubla a visão e cria mistério.
- **Simboliza:** a silhueta que toma forma na névoa, ou seja, o incerto, o que não enxergamos com clareza, os medos que aparecem quando o caminho está encoberto.
- **No jogo:** é **invisível**. Perto dela, a sanidade cai sem explicação.
- **Proteção:** **arruda revela** as visagens por 60 s. O amuleto as afasta, a barreira segura e a defumação dissipa. A camomila devolve a calma.
- **Arte:** capuz de névoa azul-esverdeada com dois olhos ciano (`spirit_visagem_0..1`).

## Saci-Pererê
Menino de uma perna só, gorro vermelho e cachimbo, que viaja nos redemoinhos. É travesso, não maldoso. 31 de outubro é o Dia do Saci.
- **No jogo:** rápido, rouba um item aleatório e some. O cheiro do cachimbo mexe com a sanidade de quem está com fissura.
- **Proteção:** **amuleto não funciona**. Só a **guiné** (aura anti-Saci), a barreira ou a defumação o afastam.
- **Arte:** `spirit_saci_0..1` (pulinho + redemoinho).

## Olho-Gordo
Crença de que a inveja traz azar, cansaço e planta murcha. Benzedeiras tiram o olhado com raminho de arruda.
- **No jogo:** de longe (4,5 m), lança mau-olhado (colheita pela metade, vendas −20%, aura sem regenerar). Com aura ≥ 70, você resiste.
- **Proteção:** **pimenteira** (aura anti-Olho-Gordo), amuleto. O raminho de arruda e o banho de descarrego limpam a uruca.
- **Arte:** olho com íris verde-inveja, pálpebra roxa com cílios de espinho e asinhas de folha (`spirit_olhogordo_0..1`).

## Lendas da Ilha (a partir do dia 5)
Sugestão do encontro Garapuvu de 28/10/2026: trazer mais folclore, como a Cuca e a Bernúncia. A partir do dia 5 (`ProtectionRules.DiaDasLendas`), cada aparição tem 25% de chance de ser uma lenda, uma de cada vez.

### Cuca
Vem da "Coca" ibérica e das cantigas de ninar ("Nana, neném, que a Cuca vem pegar..."); Monteiro Lobato a fez bruxa com cabeça de jacaré no Sítio do Picapau Amarelo.
- **Simboliza:** o medo que manda criança para a cama; a noite é para descansar.
- **No jogo:** só aparece de **madrugada** (22h às 4h) e vai atrás de quem está acordada. Ao te alcançar: −25 de energia, −10 de sanidade e **Cansaço**. Depois vai embora.
- **Proteção:** dormir antes das 22h; amuleto de arruda, barreira e defumação. Camomila e erva-cidreira ajudam o sono.
- **Arte:** bruxa-jacaré de cabelo cor de palha, vestido roxo de estrelinhas e saco de lua (`spirit_cuca_0..1`, o 1 pisca).

### Bernúncia
Personagem do **Boi-de-Mamão**, a brincadeira de rua do litoral catarinense e de Florianópolis, de origem açoriana: um bicho de pano de bocarra enorme que "engole" as crianças da roda e depois as devolve.
- **Simboliza:** o susto de brincadeira feito em comunidade, o folclore vivo da Ilha.
- **No jogo:** à noite, vai direto para a **horta** e engole um canteiro plantado (a terra continua preparada). Se te alcança, te engole e te **cospe na porta da Casa da Vó** (−10 de energia). De barriga cheia, vai embora.
- **Proteção:** o **amuleto não funciona**. A barreira de Espada-de-São-Jorge ou Comigo-ninguém-pode perto da horta deixa os canteiros fora do alcance dela; a defumação a espanta.
- **Arte:** corpo de chita listrada com bolinhas, cabeça turquesa de boca aberta, olhos esbugalhados e pernas de dançarinos embaixo do pano (`spirit_bernuncia_0..1`, o 1 fecha a boca).

## Regras (testadas em `ProtectionRulesTests` e `PlaytestGarapuvuTests`)
| Proteção | Alma | Visagem | Saci | Olho-Gordo | Cuca | Bernúncia |
|---|---|---|---|---|---|---|
| Barreira (Espada / Comigo-ninguém-pode) | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ (protege os canteiros) |
| Pimenteira | — | — | — | ✅ | — | — |
| Guiné plantada | — | — | ✅ | — | — | — |
| Amuleto de arruda | ✅ foge | ✅ foge | ❌ | ✅ foge / bloqueia | ✅ foge | ❌ |
| Defumação (raio 5–8 m) | bane | bane | bane | bane | bane | bane |

Spawn: a cada 8 s à noite. Nenhum nas duas primeiras noites; depois até `min(8, (dia − 3) / 2 + 1)` espíritos (1 no dia 3 e 4, 2 no 5 e 6, ... 8 a partir do dia 17), na mata ou num anel ao redor da jogadora. Probabilidades: Alma 35% · Visagem 20% · Saci 25% · Olho-Gordo 20%; do dia 5 em diante, 25% das vezes vem a Cuca (madrugada) ou a Bernúncia. Aura baixa e tosse aumentam a distância em que eles te percebem.
