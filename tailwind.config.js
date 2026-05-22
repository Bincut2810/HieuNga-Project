/** @type {import('tailwindcss').Config} */
module.exports = {
  content: [
    './src/HieuNga.Web/Pages/**/*.cshtml',
    './src/HieuNga.Web/ViewComponents/**/*.cshtml'
  ],
  theme: {
    extend: {
      colors: {
        honda: {
          red: '#E40521',
          dark: '#1A1A1A',
          charcoal: '#2D2D2D',
          gray: '#F5F5F5'
        }
      },
      fontFamily: {
        sans: ['Inter', 'system-ui', 'sans-serif']
      }
    }
  },
  plugins: []
};
