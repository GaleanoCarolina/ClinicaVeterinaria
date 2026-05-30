CLÍNICA VETERINARIA PATITAS

Este proyecto es un sistema de escritorio para la gestión de una clínica veterinaria. Fue desarrollado en C# con Windows Forms y utiliza una base de datos MySQL.

El sistema permite manejar información básica de la clínica, como clientes, mascotas, citas, servicios y usuarios del sistema.

---

## REQUISITOS

Para poder ejecutar el proyecto se necesita tener instalado:

* Visual Studio
* .NET
* MySQL o XAMPP
* phpMyAdmin, en caso de usar XAMPP

---

## BASE DE DATOS

La base de datos se encuentra en la carpeta:

DatosBase

Dentro de esa carpeta está el archivo SQL que debe importarse en MySQL o phpMyAdmin.

El archivo SQL ya incluye la creación de la base de datos y las tablas, por lo que no es necesario crear la base manualmente antes de importarlo.

Pasos para importar la base de datos:

1. Abrir XAMPP.
2. Iniciar Apache y MySQL.
3. Entrar a phpMyAdmin.
4. Ir a la opción Importar.
5. Seleccionar el archivo SQL que está en la carpeta DatosBase.
6. Ejecutar la importación.

Después de importar la base de datos, ya se puede abrir el proyecto en Visual Studio.

---

## EJECUCIÓN DEL PROYECTO

Para ejecutar el sistema:

1. Abrir Visual Studio.
2. Abrir el archivo de solución o el archivo del proyecto.
3. Verificar que MySQL esté iniciado.
4. Compilar y ejecutar el proyecto.

Si la conexión a la base de datos falla, revisar que el nombre de la base de datos, el usuario y la contraseña coincidan con los datos configurados en el proyecto.

---

## ESTRUCTURA DEL PROYECTO

El proyecto contiene las siguientes carpetas principales:

* Assets: contiene imágenes y recursos usados en la interfaz.
* Data: contiene archivos relacionados con la conexión o manejo de datos.
* Forms: contiene los formularios del sistema.
* Models: contiene las clases utilizadas para representar la información.
* Services: contiene parte de la lógica del sistema.
* Utils: contiene funciones de apoyo.
* DatosBase: contiene el archivo SQL de la base de datos.

---

## USUARIOS DE PRUEBA

Para ingresar al sistema se pueden utilizar los siguientes usuarios:

* admin
* recepcion
* vetagenda1
* vetagenda2
* cajaprueba

La contraseña para todos los usuarios es:

AdminVet2026!


---

## NOTA

Antes de ejecutar el proyecto, es importante importar la base de datos y tener activo el servicio de MySQL. Si se mueve el proyecto a otra computadora, también se debe importar nuevamente el archivo SQL incluido en la carpeta DatosBase.
