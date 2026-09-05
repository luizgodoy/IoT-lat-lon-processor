# Casos de teste do Harness

| ID | Cenário | Resultado esperado |
|---|---|---|
| TC01 | Abrir CSV UTF-8 | Cabeçalhos e prévia exibidos |
| TC02 | Abrir CSV `;` | Delimitador detectado |
| TC03 | Abrir XLSX | Colunas exibidas |
| TC04 | Escolher latitude/longitude | Mapeamento salvo |
| TC05 | Latitude 91 | Registro inválido |
| TC06 | Longitude 181 | Registro inválido |
| TC07 | Lat=120, Lon=-45 | Sistema detecta swap possível/necessário |
| TC08 | Lat=-23.55, Lon=-46.63 | Não inverter silenciosamente |
| TC09 | Coordenada duplicada 100x | Cache reduz chamadas |
| TC10 | Provider retorna sucesso | `Endereço` preenchido |
| TC11 | Provider retorna not found | Linha marcada sem endereço |
| TC12 | Provider retorna 429 | Respeita retry/rate limit |
| TC13 | Erro em uma linha | Demais linhas continuam |
| TC14 | Cancelar processamento | Status Cancelled e checkpoint |
| TC15 | Retomar | Não repetir linhas concluídas desnecessariamente |
| TC16 | Histórico | Job aparece na pesquisa |
| TC17 | Abrir pasta do job | Pasta correta aberta |
| TC18 | `Endereço` já existe | Usuário decide substituir/renomear/cancelar |
| TC19 | Arquivo grande | UI permanece responsiva |
| TC20 | Publish em máquina limpa | Executável inicia sem runtime .NET instalado |
| TC21 | Config externo | Alteração válida sem recompilar |
| TC22 | Falha de Internet | Erros classificados e processamento não corrompido |
| TC23 | Cancelamento durante HTTP | Task termina sem deixar estado inconsistente |
| TC24 | XLSX múltiplas planilhas | Usuário consegue selecionar planilha |
| TC25 | Arquivo inválido | Mensagem clara, sem crash |
