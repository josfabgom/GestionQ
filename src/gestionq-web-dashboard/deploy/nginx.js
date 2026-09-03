const { NodeSSH } = require('node-ssh');

const ssh = new NodeSSH();
const host = '179.199.138.79';
const username = 'root';
const password = 'Herrera4480+';

const nginxConfig = `
server {
    listen 80;
    server_name galopsrl.com.ar www.galopsrl.com.ar dashboard.galopsrl.com.ar;

    location / {
        proxy_pass http://localhost:3000;
        proxy_http_version 1.1;
        proxy_set_header Upgrade $http_upgrade;
        proxy_set_header Connection 'upgrade';
        proxy_set_header Host $host;
        proxy_cache_bypass $http_upgrade;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
    }
}
`;

async function setupNginx() {
    try {
        console.log('1. Conectando al VPS...');
        await ssh.connect({ host, username, password });
        
        console.log('2. Instalando Nginx y Certbot...');
        await ssh.execCommand('apt-get update && apt-get install -y nginx certbot python3-certbot-nginx');
        
        console.log('3. Creando archivo de configuración...');
        await ssh.execCommand(`cat << 'EOF' > /etc/nginx/sites-available/gestionq
${nginxConfig}
EOF`);

        console.log('4. Activando configuración y reiniciando Nginx...');
        await ssh.execCommand('ln -sf /etc/nginx/sites-available/gestionq /etc/nginx/sites-enabled/');
        await ssh.execCommand('rm -f /etc/nginx/sites-enabled/default');
        await ssh.execCommand('systemctl restart nginx');
        
        console.log('Nginx configurado con éxito.');
        process.exit(0);
    } catch (error) {
        console.error(error);
        process.exit(1);
    } finally {
        ssh.dispose();
    }
}

setupNginx();
