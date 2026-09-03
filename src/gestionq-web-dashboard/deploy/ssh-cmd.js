const { NodeSSH } = require('node-ssh');

const ssh = new NodeSSH();
const host = '179.199.138.79';
const username = 'root';
const password = 'Herrera4480+';

async function run() {
    try {
        await ssh.connect({
            host: host,
            username: username,
            password: password,
            readyTimeout: 10000
        });
        
        const cmd = process.argv.slice(2).join(' ');
        console.log(`Ejecutando: ${cmd}`);
        
        const result = await ssh.execCommand(cmd);
        console.log(result.stdout);
        if (result.stderr) console.error('STDERR:', result.stderr);
        
        process.exit(0);
    } catch (error) {
        console.error('Error:', error);
        process.exit(1);
    } finally {
        ssh.dispose();
    }
}
run();
