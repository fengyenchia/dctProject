import { DatabaseSync } from 'node:sqlite';
import { resolve, join } from 'node:path';
import { existsSync } from 'node:fs';
import { createDeliveryService } from './delivery-service.js';

const dataDir = resolve('data');
if (!existsSync(join(dataDir, 'tutorial.sqlite'))) throw new Error('Start I1 Gateway and save a result first');
const db = new DatabaseSync(join(dataDir, 'tutorial.sqlite'));
try {
  const service = createDeliveryService({ db, dataDir });
  const [command, id, flag] = process.argv.slice(2);
  let result: unknown;
  if (command === 'list') result = service.list();
  else if (!id) throw new Error('Usage: delivery list | status RESULT_ID | handoff RESULT_ID --simulate/--printed | retry-upload RESULT_ID | reprint RESULT_ID');
  else if (command === 'status') result = service.getStatus(id);
  else if (command === 'handoff' && ['--simulate','--printed'].includes(flag))
    result = service.confirmHandoff(id, flag === '--simulate' ? 'simulated' : 'printed');
  else if (command === 'retry-upload') { service.retryUpload(id); result = service.getStatus(id); }
  else if (command === 'reprint') result = await service.reprint(id);
  else throw new Error('Unknown delivery command');
  console.log(JSON.stringify(result, null, 2));
} finally { db.close(); }
