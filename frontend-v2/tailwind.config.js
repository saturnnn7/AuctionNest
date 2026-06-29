/** @type {import('tailwindcss').Config} */
module.exports = {
    content: ['./src/**/*.{html,ts}'],
    darkMode: 'class',
    theme: {
        extend: {
            fontFamily: {
                sans: ['Inter', 'system-ui', 'sans-serif'],
            },
            colors: {
                accent: {
                    DEFAULT: '#2563EB',
                    dark: '#1D4ED8',
                    light: '#3B82F6',
                },
                surface: {
                    light: '#F8FAFC',
                    dark: '#1E293B',
                },
                border: {
                    light: '#E2E8F0',
                    dark: '#334155',
                },
            },
        },
    },
    plugins: [],
};

