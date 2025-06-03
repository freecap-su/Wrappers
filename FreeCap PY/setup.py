from setuptools import setup, find_packages

with open("README.md", "r", encoding="utf-8") as fh:
    long_description = fh.read()

setup(
    name="freecap-client",
    version="1.0.0",
    author="FreeCap Client",
    author_email="support@freecap.su",
    description="A robust, production-ready async client for the FreeCap captcha solving service",
    long_description=long_description,
    long_description_content_type="text/markdown",
    url="https://github.com/freecap-su/Wrappers",
    packages=find_packages(),
    classifiers=[
        "Development Status :: 5 - Production/Stable",
        "Intended Audience :: Developers",
        "Topic :: Software Development :: Libraries :: Python Modules",
        "Topic :: Internet :: WWW/HTTP",
        "Topic :: Security",
        "License :: OSI Approved :: GNU General Public License v3 (GPLv3)",
        "Programming Language :: Python :: 3",
        "Programming Language :: Python :: 3.10",
        "Programming Language :: Python :: 3.11",
        "Programming Language :: Python :: 3.12",
        "Operating System :: OS Independent",
    ],
    python_requires=">=3.10",
    install_requires=[
        "aiohttp>=3.12.7",
    ],
    extras_require={
        "dev": [
            "pytest>=8.4.0",
            "pytest-asyncio>=1.0.0",
            "black>=25.1.0",
            "flake8>=7.2.0",
            "mypy>=1.16.0",
        ],
    },
    keywords="captcha, hcaptcha, funcaptcha, geetest, automation, async, api-client",
    project_urls={
        "Bug Reports": "https://github.com/freecap-su/Wrappers/issues",
        "Source": "https://github.com/freecap-su/Wrappers",
        "Documentation": "https://freecap.su/docs",
    },
) 