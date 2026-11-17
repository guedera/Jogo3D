# SkyRacers

Corrida aérea solo em um F-18: atravesse o cânion sem bater nem ultrapassar suas bordas e alcance a chegada no menor tempo possível.

## Jogar no Unity

1. Abra `SkyRacers/` no **Unity 6000.6.6f1** e aguarde a importação/compilação.
2. Abra **Assets/Scenes/CanyonRun.unity**.
3. Aperte **Play** e clique em **CANYON 01 / VOAR**.

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

A potência permanece no valor escolhido ao soltar Shift/Ctrl. Incline o avião com A/D para fazer curvas; Q/E ajudam a ajustar a direção. Use os airbrakes antes de curvas fechadas. Baixa velocidade reduz sustentação.

## Implementado

- Câmera traseira suavizada com detecção de paredes.
- Empuxo, arrasto, sustentação, inércia e assistência nas curvas.
- F-18 texturizado com estabilizadores e lemes animados, asas estendidas e trem recolhido.
- Pós-combustão a 100% de potência, chamas azuis ancoradas nos bocais e velocidade de equilíbrio em reta de aproximadamente 1.315 km/h (Mach 1,07 na atmosfera simplificada do jogo).
- Cânion alpino fixo, 10% mais estreito, com pinheiros, riacho, neve nas encostas, montanhas e variações de elevação.
- Uma única ponte de três arcos no meio da fase; passagem obrigatória sob o arco central.
- Teto de voo a 150 unidades acima do relevo local, marcadores e chegada.
- HUD de tempo, recorde, velocidade em km/h e Mach, potência, altitude e progresso.
- Preparação, pausa, derrota, vitória, reinício e menu principal.
- Recorde local salvo somente após completar todos os setores.
- Áudio de motor gerado por código e efeitos visuais de escapamento.

Colidir, ultrapassar o teto, contornar a passagem central da ponte ou sair pelos limites longitudinais encerra a run. Pausar congela cronômetro e física; perder o foco da janela também pausa. Reiniciar restaura a run desde o começo. A nova fase tem uma chave própria de recorde para não comparar tempos com a versão antiga.

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

A versão inicial foi testada pelo usuário, que aprovou câmera e dirigibilidade. Esta revisão preserva os parâmetros de rotação e a câmera, altera a propulsão e alonga o percurso para aproximadamente 46,3 km. O tempo idealizado à velocidade máxima é 127 segundos; aceleração, curvas e frenagens aumentam esse valor. A duração da nova run ainda precisa ser medida em voo.

As sombras do perfil PC passaram de 50 para 600 unidades de distância, com atlas de 4096 e quatro cascatas. O perfil Mobile usa 300 unidades e atlas de 2048. A melhoria visual e o custo de desempenho precisam ser avaliados em Play Mode.

Playtest inicial:

1. Iniciar e conferir câmera, todos os controles e movimentos das superfícies.
2. Pausar, continuar e reiniciar; conferir cronômetro e potência restaurados.
3. Bater no piso e nas paredes, ultrapassar o teto e reiniciar após cada derrota.
4. Completar a fase e conferir o recorde após sair e abrir novamente.
5. Testar a build Windows e ajustar sensibilidade/velocidade conforme a pilotagem.

`Tools/verify_project.py` repete a compilação estática e a validação das referências no macOS, após a primeira importação do Unity. `Tools/convert_aircraft.py` regenera os dados do modelo. `Tools/create_bootstrap_scene.py` regenera e sobrescreve a cena inicial e seus materiais.

## Assets e créditos

| Recurso | Origem |
| --- | --- |
| Modelo e texturas do F-18 | `low-poly-f18-super-hornet.zip`, fornecido pelo usuário; glTF exportado pelo Blockbench |
| Cenário, interface, código e áudio procedural | Criados para SkyRacers |

O ZIP não inclui autor, URL de origem ou licença. Registrar esses dados antes da publicação. O modelo original está preservado em `Assets/ThirdParty/F18/model.gltf`; o jogo usa dados convertidos com a hierarquia das peças preservada.

Referências técnicas: [especificação glTF](https://registry.khronos.org/glTF/specs/2.0/glTF-2.0.html) e [Rigidbody do Unity](https://docs.unity.com/en-us/engine/6000.0/script-reference/unityengine/rigidbody).

## Versionamento

Versionar `Assets/` com os arquivos `.meta`, `Packages/` e `ProjectSettings/`. O `.gitignore` exclui caches, configurações locais, arquivos gerados pelas IDEs e builds.
