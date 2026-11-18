# SkyRacers

Corrida aérea solo em um F-18: atravesse o cânion sem bater nem ultrapassar suas bordas e alcance a chegada no menor tempo possível.

## Jogar no Unity

1. Abra `SkyRacers/` no **Unity 6000.6.6f1** e aguarde a importação/compilação.
2. Abra **Assets/Scenes/CanyonRun.unity**.
3. Aperte **Play** e escolha **CANYON 01 / VOAR** ou **VOO LIVRE / TESTAR FÍSICA**.

Voo livre começa acima do cânion, sem contagem inicial, teto de altitude, setores obrigatórios, cronômetro ou chegada. As colisões permanecem ativas. ESC permite continuar, reiniciar no mesmo modo ou voltar ao menu.

A cena constrói o cenário e o avião ao iniciar. É normal ver apenas o objeto de inicialização na Hierarchy antes do Play. Não é necessário instalar um importador glTF nem configurar objetos manualmente.

## Controles

| Tecla | Ação |
| --- | --- |
| Shift / Ctrl, segurados | Aumentar / diminuir potência |
| X, segurado | Airbrakes |
| A / D | Roll para esquerda / direita |
| S / W | Levantar / abaixar o nariz |
| Q / E | Yaw para esquerda / direita |
| Esc | Pausar / continuar |

A potência permanece no valor escolhido ao soltar Shift/Ctrl. Incline o avião com A/D e puxe com S para fechar a curva; Q/E ajudam a ajustar a direção. Use os airbrakes antes de curvas fechadas. Baixa velocidade reduz sustentação.

## Implementado

- Câmera traseira suavizada com detecção de paredes.
- Sustentação por velocidade/densidade/ângulo de ataque, arrasto induzido, estol progressivo e controle de taxa angular com proteção de ângulo de ataque/carga G. Roll de até 150°/s, com resposta mais rápida e limites de carga de −5 a +10,5 G (ajustados para o jogo).
- F-18 texturizado com estabilizadores, ailerons, flaps e lemes animados, asas estendidas e trem recolhido.
- Pós-combustão a 100% de potência e chamas azuis ancoradas nos bocais. O ensaio longitudinal isolado chega a Mach 1,29 após 20 s de potência máxima, partindo de 170 m/s a 600 m; não é uma medição em Play Mode.
- Cânion alpino fixo, 60% mais estreito que a versão alpina anterior, com pinheiros, riacho, neve nas encostas, montanhas e variações de elevação.
- Uma única ponte de três arcos no meio da fase; passagem obrigatória sob o arco central.
- Teto de voo a 450 unidades acima do relevo local, duas cachoeiras e chegada sem postes.
- HUD lateral com fitas móveis de velocidade indicada (km/h) e altitude AGL (m), potência, Mach e distância restante ao longo do percurso. Marcações deslizam sob índices fixos; centro livre. Cronômetro e recorde aparecem no resultado.
- Preparação, pausa, derrota, vitória, reinício e menu principal.
- Recorde local salvo somente após completar todos os setores.
- Áudio de motor gerado por código e efeitos visuais de escapamento.

No modo corrida, colidir, ultrapassar o teto, contornar a passagem central da ponte ou sair pelos limites longitudinais encerra a run. Pausar congela cronômetro e física; perder o foco da janela também pausa. Reiniciar restaura a run desde o começo. A nova fase tem uma chave própria de recorde para não comparar tempos com a versão antiga.

## Editar e gerar build

O menu **SkyRacers** aparece após a compilação:

| Comando | Uso |
| --- | --- |
| 1 - Build Game Scene | Gera prefab e meshes persistentes e substitui CanyonRun pela versão editável |
| 2 - Open Game Scene | Abre a cena do jogo |
| 3 - Build Windows | Gera `SkyRacers/Builds/Windows/SkyRacers.exe` |
| 4 - Validate Course | Verifica piso e setores na cena editável gerada pelo comando 1 |

O comando 1 reconstrói a cena: salve em outra cena qualquer edição manual que queira preservar. É opcional; a cena inicial já pode ser executada e compilada diretamente.

Parâmetros de voo: `FlightController`. Traçado e dimensões: `CanyonCourse`. Superfícies móveis: `AircraftFactory`. Planejamento: [GDD.md](GDD.md).

## Verificação

Concluído: compilação C# dos scripts de jogo e editor contra as assemblies instaladas do Unity 6000.6.6f1; referências da cena/materiais; índices, UVs e pose das asas do modelo; inspeção geométrica das peças.

Não foi possível executar Play Mode nem gerar a build neste ambiente: a execução adicional do Unity foi impedida por restrições aos serviços locais do editor. Visual, shaders, física em execução, FPS e build Windows precisam de validação no editor.

A versão inicial foi testada pelo usuário. A revisão atual altera a física e mantém a câmera. O percurso tem aproximadamente 46,3 km; a duração precisa ser medida com o novo modelo de voo. Os coeficientes são ajustados para o jogo e não constituem uma simulação certificada do F/A-18.

As sombras do perfil PC passaram de 50 para 600 unidades de distância, com atlas de 4096 e quatro cascatas. O perfil Mobile usa 300 unidades e atlas de 2048. A melhoria visual e o custo de desempenho precisam ser avaliados em Play Mode.

Playtest inicial:

1. Iniciar e conferir câmera, todos os controles e movimentos das superfícies.
2. Pausar, continuar e reiniciar; conferir cronômetro e potência restaurados.
3. Bater no piso e nas paredes, ultrapassar o teto e reiniciar após cada derrota.
4. Completar a fase e conferir o recorde após sair e abrir novamente.
5. Testar a build Windows e ajustar sensibilidade/velocidade conforme a pilotagem.

`Tools/verify_project.py` repete a compilação, valida referências e executa `Tools/FlightChecks.cs` contra as equações reais de voo e do HUD: trim, sentido dos comandos, limites G, estol, frenagem, pós-combustão e continuidade das escalas. Requer a primeira importação do Unity no macOS. `Tools/convert_aircraft.py` regenera os dados do modelo. `Tools/create_bootstrap_scene.py` regenera e sobrescreve a cena inicial e seus materiais.

## Assets e créditos

| Recurso | Origem |
| --- | --- |
| Modelo e texturas do F-18 | `low-poly-f18-super-hornet.zip`, fornecido pelo usuário; glTF exportado pelo Blockbench |
| Cenário, interface, código e áudio procedural | Criados para SkyRacers |

O ZIP não inclui autor, URL de origem ou licença. Registrar esses dados antes da publicação. O modelo original está preservado em `Assets/ThirdParty/F18/model.gltf`; o jogo usa dados convertidos com a hierarquia das peças preservada.

Referências técnicas: [especificação glTF](https://registry.khronos.org/glTF/specs/2.0/glTF-2.0.html) e [Rigidbody do Unity](https://docs.unity.com/en-us/engine/6000.0/script-reference/unityengine/rigidbody).

## Versionamento

Versionar `Assets/` com os arquivos `.meta`, `Packages/` e `ProjectSettings/`. O `.gitignore` exclui caches, configurações locais, arquivos gerados pelas IDEs e builds.


## Revisão de efeitos e cenário — 18/11/2026

- Profundidade triplicada para 450 m, largura reduzida para aproximadamente 59–101 m no fundo. Postes removidos; chegada marcada no piso.
- Duas cachoeiras com névoa, relevo com mais segmentos, normais suavizadas e detalhe procedural na superfície para reduzir o aspecto facetado.
- Arco central de 22 m de largura e 438 m de altura máxima, com laterais abertas até 408 m e tabuleiro a 458 m. Exige alinhamento lateral; já não exige rasante de 9 m. Os pilares continuam colidindo.
- HUD lateral verde neon de altitude, velocidade e potência; centro da tela livre. Alarme pulsado de altitude que cessa em pausa, derrota ou retorno à altitude segura.
- Fumaça dos motores e trilhas nas pontas das asas com baixa opacidade.
- Impacto separa as 74 meshes em destroços com colisores, gera fogo, fumaça, fragmentos e som; aguarda 2,5 s antes do resultado. Reiniciar restaura o avião e remove destroços.
- Boom sônico e anel discreto de condensação somente na passagem de Mach < 1 para Mach >= 1 durante a run. Não dispara ao desacelerar, permanecer supersônico ou retomar de uma pausa.
- Física revisada com controle assistido de ângulo de ataque, sustentação perpendicular ao fluxo e perda de energia nas curvas. Novo identificador de recorde para separar tempos da física anterior.

Compilação C# e referências verificadas. Efeitos, som, desempenho e passagem pela ponte ainda precisam de playtest no Unity nesta revisão.


## Playtest desta revisão

1. No menu, entrar em VOO LIVRE. Subir acima de 450 m, sair lateralmente do cânion e passar pela ponte sem encerrar o voo nem tocar o alarme de teto.
2. Comparar A/D, S/W e Q/E; inclinar e puxar S para fazer curvas. Soltar o pitch e conferir a estabilização; reduzir potência e testar X.
3. Conferir as fitas do HUD deslizando continuamente tanto ao acelerar/subir quanto ao desacelerar/descer, inclusive nas mudanças de graduação.
4. Pausar e reiniciar: manter voo livre, restaurar posição/potência e limpar efeitos. Colidir e tentar novamente deve manter o modo livre.
5. Voltar ao menu e iniciar CANYON 01: confirmar contagem, distância restante diminuindo, alarme e derrota no teto; passar pelo vão central da ponte a 65, 200 e 350 m acima do piso.
6. Finalizar a corrida e conferir o recorde; voo livre nunca grava tempos.

Base das equações de sustentação e arrasto: [NASA — Lift to Drag Ratio](https://www1.grc.nasa.gov/beginners-guide-to-aeronautics/lift-to-drag-ratio/) e [NASA — Modern Drag Equation](https://www1.grc.nasa.gov/beginners-guide-to-aeronautics/modern-drag-equation/).


## Refinamento de pitch e superfícies

- W/S comandam a rotação com resposta inicial mais firme, limitada progressivamente por ângulo de ataque e carga G. O envelope −5/+10,5 G prioriza pilotagem de jogo; não representa os limites certificados do F/A-18.
- Física lê a pose do Rigidbody e aplica correções por torque com aceleração angular limitada, evitando depender da interpolação da câmera/modelo.
- Oito superfícies animadas: estabilizadores, ailerons externos, flaps internos e lemes. Flaps automáticos por velocidade/AoA, lemes com eixo inclinado e asas sem dobra durante o voo.
- Ensaios longitudinais de 2 s com potência máxima: a 612 km/h, S altera a atitude em 75,1° e W em −43,8°; a 1.224 km/h, 33,6° e −22,8°. São ensaios numéricos, não medições de Play Mode.
- Testes incluem resposta em três velocidades, comando curto, passagem pela vertical, recuperação ao soltar, passos de física de 20/50 ms e sentido/limites das superfícies.

Referências consultadas: [VAZGRIZ — Flight Simulator in Unity3D](https://vazgriz.com/346/flight-simulator-in-unity3d-part-1/), [Aircraft Physics](https://github.com/gasgiant/Aircraft-Physics), [NASA — F-18 HARV](https://www.nasa.gov/reference/f-18-harv/) e [Unity — AddTorque](https://docs.unity.com/en-us/engine/6000.0/script-reference/unityengine/rigidbody/addtorque). Implementação própria; nenhum pacote externo foi importado.

No próximo Play, testar W/S com toques curtos e depois segurando, em cruzeiro e acima de Mach 1. Verificar A/D nos ailerons e estabilizadores, Q/E nos lemes e X nos flaps; a animação deve voltar ao neutro/posição automática após soltar.
