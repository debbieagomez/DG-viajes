(function () {
    const token = document.querySelector('input[name="__RequestVerificationToken"]').value;
    const formatoMoneda = new Intl.NumberFormat('es-AR', { style: 'currency', currency: 'ARS', maximumFractionDigits: 0 });

    async function api(url, opciones = {}) {
        const headers = Object.assign({ RequestVerificationToken: token }, opciones.headers || {});
        const resp = await fetch(url, Object.assign({}, opciones, { headers, credentials: 'same-origin' }));
        let datos = null;
        try { datos = await resp.json(); } catch (e) { /* respuesta sin cuerpo */ }
        if (!resp.ok) {
            const err = new Error('http ' + resp.status);
            err.errores = (datos && datos.errores) || ['Ocurrió un error inesperado (' + resp.status + '). Probá de nuevo.'];
            throw err;
        }
        return datos;
    }

    // Cuadro de búsqueda con resultados que vienen del servidor (AJAX).
    const Buscador = {
        props: {
            url: { type: String, required: true },
            params: { type: Object, default: () => ({}) },
            modelValue: { type: Object, default: null },
            placeholder: String,
            inputId: String
        },
        emits: ['update:modelValue'],
        data() { return { q: '', opciones: [], abierto: false, cargando: false, error: '', temporizador: null }; },
        methods: {
            escribir() { clearTimeout(this.temporizador); this.temporizador = setTimeout(this.buscar, 250); },
            async buscar() {
                this.cargando = true;
                this.error = '';
                const p = new URLSearchParams(Object.assign({}, this.params, { q: this.q }));
                try { this.opciones = await api(this.url + '?' + p.toString()); }
                catch (e) { this.error = e.errores.join(' '); this.opciones = []; }
                finally { this.cargando = false; }
            },
            abrir() { this.abierto = true; this.buscar(); },
            cerrar() { this.abierto = false; },
            elegir(o) { this.$emit('update:modelValue', o); this.abierto = false; this.q = ''; },
            quitar() { this.$emit('update:modelValue', null); this.q = ''; this.opciones = []; }
        },
        template: `
        <div class="position-relative">
            <div v-if="modelValue" class="d-flex justify-content-between align-items-start border rounded p-2">
                <div><slot name="elegido" :o="modelValue"></slot></div>
                <button type="button" class="btn btn-sm btn-outline-secondary" v-on:click="quitar">Cambiar</button>
            </div>
            <div v-else>
                <input :id="inputId" class="form-control" type="search" autocomplete="off" :placeholder="placeholder"
                       v-model="q" v-on:input="escribir" v-on:focus="abrir" v-on:blur="cerrar">
                <div v-if="abierto" class="list-group position-absolute w-100 shadow-sm" style="z-index: 1050">
                    <div v-if="cargando" class="list-group-item text-muted">Buscando...</div>
                    <div v-else-if="error" class="list-group-item text-danger">{{ error }}</div>
                    <div v-else-if="opciones.length === 0" class="list-group-item text-muted">No hay resultados disponibles.</div>
                    <button v-for="o in opciones" :key="o.id" type="button" class="list-group-item list-group-item-action"
                            v-on:mousedown.prevent="elegir(o)"><slot name="opcion" :o="o"></slot></button>
                </div>
            </div>
        </div>`
    };

    const app = Vue.createApp({
        data() {
            return { cliente: null, paquete: null, hospedaje: null, cantPasajeros: 1, acompanantes: [], errores: [], guardando: false };
        },
        computed: {
            maxPasajeros() {
                const topes = [];
                if (this.paquete) topes.push(this.paquete.plazasLibres);
                if (this.hospedaje) topes.push(this.hospedaje.lugaresLibres);
                return topes.length ? Math.max(1, Math.min(...topes)) : 1;
            },
            totalPaquete() { return this.paquete ? this.paquete.precioPersona * this.cantPasajeros : 0; },
            totalHospedaje() { return this.paquete && this.hospedaje ? this.hospedaje.precioNoche * this.paquete.noches : 0; },
            total() { return this.totalPaquete + this.totalHospedaje; },
            listo() { return !!(this.cliente && this.paquete && this.hospedaje); }
        },
        watch: {
            paquete() { this.hospedaje = null; this.ajustar(); },
            hospedaje() { this.ajustar(); },
            cantPasajeros() { this.ajustar(); }
        },
        methods: {
            moneda(n) { return formatoMoneda.format(n); },
            fecha(s) { return s ? s.substring(0, 10).split('-').reverse().join('/') : ''; },
            // Mantiene la cantidad de pasajeros dentro del tope y una fila de acompañante por cada pasajero extra.
            ajustar() {
                let n = Math.floor(Number(this.cantPasajeros)) || 1;
                n = Math.max(1, Math.min(n, this.maxPasajeros));
                if (n !== this.cantPasajeros) { this.cantPasajeros = n; return; }
                const faltan = n - 1;
                while (this.acompanantes.length < faltan) this.acompanantes.push({ dni: '', nombre: '', apellido: '', telefono: '' });
                if (this.acompanantes.length > faltan) this.acompanantes.splice(faltan);
            },
            async guardar() {
                this.errores = [];
                if (!this.listo) { this.errores = ['Elegí un cliente, un paquete y un hospedaje.']; return; }
                this.guardando = true;
                try {
                    const r = await api('/Reservas/Guardar', {
                        method: 'POST',
                        headers: { 'Content-Type': 'application/json' },
                        body: JSON.stringify({
                            clienteId: this.cliente.id, paqueteId: this.paquete.id, hospedajeId: this.hospedaje.id,
                            cantPasajeros: this.cantPasajeros, acompanantes: this.acompanantes
                        })
                    });
                    window.location.href = '/Reservas/Detalle/' + r.id;
                } catch (e) {
                    this.errores = e.errores;
                    this.guardando = false;
                    window.scrollTo(0, 0);
                }
            }
        }
    });
    app.component('buscador', Buscador);
    app.mount('#app');
})();
