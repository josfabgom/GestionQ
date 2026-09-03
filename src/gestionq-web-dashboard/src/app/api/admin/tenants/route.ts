import { NextRequest, NextResponse } from "next/server";
import { getServerSession } from "next-auth/next";
import { authOptions } from "../../auth/[...nextauth]/route";
import { PrismaClient } from "@prisma/client";
import bcrypt from "bcryptjs";
import crypto from "crypto";

const prisma = new PrismaClient();

export async function POST(req: NextRequest) {
  try {
    const session = await getServerSession(authOptions);

    if (!session || session.user.role !== "ADMIN") {
      return NextResponse.json({ error: "No autorizado" }, { status: 403 });
    }

    const { companyName, email, password } = await req.json();

    if (!companyName || !email || !password) {
      return NextResponse.json({ error: "Faltan datos requeridos" }, { status: 400 });
    }

    // Check if email already exists
    const existingUser = await prisma.user.findUnique({ where: { email } });
    if (existingUser) {
      return NextResponse.json({ error: "El email ya está en uso" }, { status: 400 });
    }

    // Generate unique API Key
    const apiKey = "gq_" + crypto.randomBytes(24).toString("hex");

    // Hash the password
    const passwordHash = await bcrypt.hash(password, 10);

    // Create Tenant and User in a transaction
    const tenant = await prisma.tenant.create({
      data: {
        companyName,
        apiKey,
        users: {
          create: {
            name: "Admin " + companyName,
            email,
            passwordHash,
            role: "CLIENT"
          }
        }
      },
      include: {
        users: true
      }
    });

    return NextResponse.json({ success: true, tenant });
  } catch (error) {
    console.error("Admin Create Tenant Error:", error);
    return NextResponse.json({ error: "Error interno del servidor" }, { status: 500 });
  }
}

export async function GET(req: NextRequest) {
  try {
    const session = await getServerSession(authOptions);

    if (!session || session.user.role !== "ADMIN") {
      return NextResponse.json({ error: "No autorizado" }, { status: 403 });
    }

    const tenants = await prisma.tenant.findMany({
      include: {
        users: {
          select: { email: true }
        }
      },
      orderBy: { createdAt: 'desc' }
    });

    return NextResponse.json(tenants);
  } catch (error) {
    return NextResponse.json({ error: "Error al obtener clientes" }, { status: 500 });
  }
}
