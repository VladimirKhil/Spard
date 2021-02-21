SPARD service web client.

Example usage:

```typescript
import SpardClient from 'spard-client';

const client = new SpardClient({ serviceUri: '<insert service address here>' });
const result = await client.transformAsync({ input: 'aaa', transform: 'a => b' });

console.log(result); // { result: 'bbb', duration: '...' }
```

For more information please visit https://github.com/VladimirKhil/Spard/wiki.