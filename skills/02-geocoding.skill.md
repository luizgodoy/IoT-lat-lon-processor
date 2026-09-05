# SKILL — Geocoding

Ao implementar reverse geocoding:

1. Valide latitude/longitude antes da rede.
2. Normalize decimal cuidadosamente.
3. Detecte inversão somente quando sustentada pelos limites ou confirmação do usuário.
4. Use cache por coordenada.
5. Respeite rate limit configurado.
6. Envie User-Agent identificável.
7. Use timeout.
8. Faça retry apenas para falhas transitórias.
9. Registre métricas sem registrar dados desnecessários.
10. Trate `not found` como resultado de negócio, não como crash.
11. Mantenha o provedor atrás de `IGeocodingProvider`.
12. Antes do release, valide a política atual do serviço escolhido.

O agente deve preferir precisão e conformidade ao provedor em vez de maximizar paralelismo.
