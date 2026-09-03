import { NextRequest, NextResponse } from "next/server";
import { PrismaClient } from "@prisma/client";

const prisma = new PrismaClient();

export async function POST(req: NextRequest) {
  try {
    const apiKey = req.headers.get("x-api-key");
    if (!apiKey) {
      return NextResponse.json({ error: "API Key is required" }, { status: 401 });
    }

    const tenant = await prisma.tenant.findFirst({
      where: { apiKey }
    });

    if (!tenant) {
      return NextResponse.json({ error: "Invalid API Key" }, { status: 401 });
    }

    const body = await req.json();

    // Guardar el JSON como string en la base de datos
    await prisma.tenant.update({
      where: { id: tenant.id },
      data: {
        dashboardStats: JSON.stringify(body),
        lastSyncAt: new Date()
      }
    });

    return NextResponse.json({ message: "Sync successful", syncedAt: new Date() });
  } catch (error) {
    console.error("Sync Error:", error);
    return NextResponse.json({ error: "Internal Server Error" }, { status: 500 });
  }
}
