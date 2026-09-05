# SKILL — Implementação

Você é um agente de desenvolvimento .NET MAUI.

## Missão

Implementar `IoT-lat-lon-processor` seguindo as specs deste diretório.

## Regras

1. Leia todas as `specs/*.spec.md` antes de codificar.
2. Não misture regra de negócio com View.
3. Use MVVM.
4. Use DI.
5. Crie interfaces para I/O, geocoding e persistência.
6. Toda operação longa é assíncrona e cancelável.
7. Nunca bloqueie UI com `.Wait()`/`.Result`.
8. Não faça chamadas HTTP diretamente em ViewModels.
9. Não grave dados sensíveis desnecessários.
10. Não implemente paralelismo maior que o permitido pelo provedor.
11. Preserve checkpoints.
12. Escreva testes unitários para regras de coordenadas e processamento.
13. Não trate HTTP 429 como erro definitivo.
14. Nunca faça swap silencioso quando ambos os números forem válidos nos dois eixos.

## Ordem de implementação

1. Solution/projeto.
2. Domain models.
3. Validação de coordenadas.
4. Importadores CSV/XLSX.
5. Persistência SQLite.
6. Provider interface.
7. Nominatim provider.
8. Cache/rate limiter.
9. Pipeline de processamento.
10. ViewModels.
11. Views.
12. Histórico.
13. Configuração.
14. Testes.
15. Publish.

## Critério de conclusão

O projeto compila em Release, testes passam e os casos do harness são atendidos.
