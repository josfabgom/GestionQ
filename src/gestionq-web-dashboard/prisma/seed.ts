import { PrismaClient } from '@prisma/client'
import bcrypt from 'bcryptjs'

const prisma = new PrismaClient()

async function main() {
  const passwordHash = await bcrypt.hash('admin123', 10)

  const tenant = await prisma.tenant.create({
    data: {
      companyName: 'Empresa Demo',
      cloudflareTunnelUrl: 'https://nail-straining-cackle.ngrok-free.dev', // Using the ngrok URL for now
      apiKey: 'tu-super-secreta-api-key-para-el-dashboard',
      users: {
        create: {
          name: 'Admin',
          email: 'admin@gestionq.com',
          passwordHash: passwordHash,
          role: 'ADMIN',
        },
      },
    },
  })

  console.log('Seed completed. Created user admin@gestionq.com / admin123')
}

main()
  .then(async () => {
    await prisma.$disconnect()
  })
  .catch(async (e) => {
    console.error(e)
    await prisma.$disconnect()
    process.exit(1)
  })
