const { NodeSSH } = require('node-ssh');
const ssh = new NodeSSH();
async function run() {
    await ssh.connect({ host: '179.199.138.79', username: 'root', password: 'Herrera4480+' });
    const result = await ssh.execCommand("cd /root/gestionq-web-dashboard && node -e 'const { PrismaClient } = require(\"@prisma/client\"); const prisma = new PrismaClient(); prisma.tenant.findFirst().then(t => console.log(JSON.stringify(JSON.parse(t.dashboardStats).topProducts).substring(0, 500)))'");
    console.log(result.stdout);
    console.log(result.stderr);
    process.exit(0);
}
run();
