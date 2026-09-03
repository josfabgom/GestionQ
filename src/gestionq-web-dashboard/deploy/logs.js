const { NodeSSH } = require('node-ssh');

const ssh = new NodeSSH();
const host = '179.199.138.79';
const username = 'root';
const password = 'Herrera4480+';

async function fetchLogs() {
    try {
        await ssh.connect({
            host: host,
            username: username,
            password: password,
        });

        const result = await ssh.execCommand('docker logs gestionq-web-dashboard-web-1 --tail 50');
        console.log(result.stdout);
        console.log(result.stderr);
        
        process.exit(0);
    } catch (error) {
        console.error(error);
        process.exit(1);
    } finally {
        ssh.dispose();
    }
}

fetchLogs();
