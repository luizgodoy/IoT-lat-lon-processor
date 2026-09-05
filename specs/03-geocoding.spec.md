# SPEC 03 — Geocodificação e coordenadas

## 1. Provedor

Implementar `NominatimGeocodingProvider` usando a API pública do Nominatim/OpenStreetMap como provedor inicial gratuito.

A URL/base do serviço deve ser configurável. O cliente HTTP deve enviar um `User-Agent` identificando claramente a aplicação e um contato configurável.

**Importante:** o agente de implementação deve verificar as políticas atuais do serviço antes do release e não assumir que uso em lote ilimitado é permitido.

## 2. Parsing

Normalizar:
- espaços;
- sinal;
- separador decimal;
- strings vazias;
- valores com caracteres extras.

Não aceitar silenciosamente formatos ambíguos.

## 3. Regras

```text
latitude ∈ [-90, 90]
longitude ∈ [-180, 180]
```

Casos:
- lat válida + lon válida → candidata a processamento.
- lat inválida + lon válida e `lat` caberia em longitude → testar swap.
- lat válida + lon inválida e `lon` caberia em latitude → testar swap.
- ambas inválidas → erro.
- ambas válidas → não swap automático.

## 4. Heurística de inversão

Quando ambos os números são válidos nos dois papéis, usar apenas evidências adicionais:
- nomes das colunas;
- valores de amostra;
- distribuição geográfica;
- opção explícita do usuário.

Exemplo: `latitude=120`, `longitude=-45` só pode ser interpretado como `lat=-45`, `lon=120`.

Exemplo ambíguo: `-23.55, -46.63` e `-46.63, -23.55` são ambos matematicamente válidos. Não trocar silenciosamente.

## 5. Resultado

`GeocodingResult` deve conter:
- Success;
- DisplayAddress;
- Provider;
- Raw/structured response opcional apenas para diagnóstico;
- ErrorCode;
- ErrorMessage;
- HttpStatus;
- RetryAfter.

Não gravar resposta HTTP completa no log em produção sem necessidade.

## 6. Rate limiting

Implementar um `RateLimiter` centralizado por provedor.

Não disparar paralelismo irrestrito. O limite deve ser configurável e ter valor padrão conservador compatível com a política vigente do provedor.

Aplicar:
- atraso mínimo entre chamadas;
- retry com backoff para erros transitórios;
- respeito a `Retry-After`;
- cancelamento.

## 7. Cache

Cachear por coordenada normalizada:
`round(latitude, 6) + ":" + round(longitude, 6) + ":" + provider`.

O cache reduz chamadas repetidas e melhora performance. A precisão do arredondamento deve ser configurável.

## 8. Falhas

Classificar:
- invalid_coordinate;
- not_found;
- timeout;
- rate_limited;
- unauthorized;
- provider_error;
- network_error;
- cancelled.

Somente erros transitórios devem ter retry automático.
