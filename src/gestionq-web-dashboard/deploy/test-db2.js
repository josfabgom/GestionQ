const { NodeSSH } = require('node-ssh');
const ssh = new NodeSSH();
(async () => {
    await ssh.connect({ host: '179.199.138.79', username: 'root', password: 'Herrera4480+', tryKeyboard: true });
    const raw = await ssh.execCommand('docker exec gestionq-web-dashboard-web-1 node -e "const { PrismaClient } = require(\'@prisma/client\'); const p = new PrismaClient(); p.tenant.findMany().then(res => { console.log(\'lastSyncAt:\', res[0].lastSyncAt); console.log(\'stats null?\', res[0].dashboardStats === null); }).catch(console.error).finally(()=>p.\\$disconnect());"');
    console.log('Result:', raw.stdout);
    if(raw.stderr) console.error('Error:', raw.stderr);
    ssh.dispose();
})().catch(console.error);
