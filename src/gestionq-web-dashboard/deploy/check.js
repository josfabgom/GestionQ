const { NodeSSH } = require('node-ssh');
const ssh = new NodeSSH();
async function run() {
    await ssh.connect({ host: '179.199.138.79', username: 'root', password: 'Herrera4480+' });
    const result = await ssh.execCommand("docker exec gestionq-web-dashboard-web-1 node -e 'const { PrismaClient } = require(\"@prisma/client\"); const prisma = new PrismaClient(); prisma.tenant.findMany().then(ts => console.log(JSON.stringify(ts.map(t => { const s = t.dashboardStats ? JSON.parse(t.dashboardStats) : {}; return { name: t.companyName, hasTopProducts: !!s.topProducts, hasRawSales: !!s.rawSales }; }))))'");
    console.log(result.stdout);
    process.exit(0);
}
run();
