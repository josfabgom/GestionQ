const { NodeSSH } = require('node-ssh');
const path = require('path');

const ssh = new NodeSSH();
const host = '179.199.138.79';
const username = 'root';
const password = 'Herrera4480+';

const tarFile = path.join(__dirname, '..', 'deploy.tar.gz');
const remotePath = '/root/gestionq-web-dashboard';

async function deploy() {
    try {
        console.log('1. Conectando al VPS...');
        await ssh.connect({
            host: host,
            username: username,
            password: password,
            tryKeyboard: true,
            readyTimeout: 10000
        });
        console.log('Conectado al VPS.');

        console.log('2. Verificando/Instalando Docker y Docker Compose...');
        await ssh.execCommand('if ! command -v docker &> /dev/null; then curl -fsSL https://get.docker.com -o get-docker.sh && sh get-docker.sh; fi');
        await ssh.execCommand('if ! command -v docker-compose &> /dev/null; then apt-get update && apt-get install -y docker-compose || (curl -L "https://github.com/docker/compose/releases/latest/download/docker-compose-$(uname -s)-$(uname -m)" -o /usr/local/bin/docker-compose && chmod +x /usr/local/bin/docker-compose); fi');

        console.log('3. Preparando directorio remoto...');
        await ssh.execCommand(`mkdir -p ${remotePath}`);

        console.log('4. Subiendo archivo tar.gz...');
        await ssh.putFile(tarFile, `${remotePath}/deploy.tar.gz`);
        console.log('Archivo subido.');

        console.log('5. Descomprimiendo y levantando Docker...');
        const result = await ssh.execCommand(`cd ${remotePath} && rm -f prisma7.config.ts && tar -xzf deploy.tar.gz && docker-compose down && docker-compose up -d --build`, {
            onStdout: chunk => process.stdout.write(chunk.toString('utf8')),
            onStderr: chunk => process.stderr.write(chunk.toString('utf8')),
        });

        console.log('6. Resultado del despliegue:');
        console.log(result.stdout);
        console.log(result.stderr);

        console.log('Despliegue finalizado con éxito!');
        process.exit(0);
    } catch (error) {
        console.error('Error durante el despliegue:', error);
        process.exit(1);
    } finally {
        ssh.dispose();
    }
}

deploy();
