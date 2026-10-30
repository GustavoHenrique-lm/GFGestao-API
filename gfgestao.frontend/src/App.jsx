
import { useCallback, useEffect, useState } from 'react'
import './App.css'

const API_URL = 'http://localhost:5092/api'

const tarefaInicial = {
  titulo: '',
  descricao: '',
  tipo: 'Sprint',
  responsavel: '',
  data: '',
  status: 'Pendente',
}

function lerUsuarioSalvo() {
  const token = sessionStorage.getItem('gf_token')
  const dados = sessionStorage.getItem('gf_usuario')

  if (!token || !dados) return null

  try {
    return JSON.parse(dados)
  } catch {
    sessionStorage.removeItem('gf_token')
    sessionStorage.removeItem('gf_usuario')
    return null
  }
}

function dataParaInput(data) {
  if (!data) return ''

  const valor = new Date(data)

  if (Number.isNaN(valor.getTime())) return ''

  return [
    valor.getUTCFullYear(),
    String(valor.getUTCMonth() + 1).padStart(2, '0'),
    String(valor.getUTCDate()).padStart(2, '0'),
  ].join('-')
}

function App() {
  const [usuario, setUsuario] = useState(lerUsuarioSalvo)
  const [token, setToken] = useState(
    () => sessionStorage.getItem('gf_token') || ''
  )

  const [email, setEmail] = useState('')
  const [senha, setSenha] = useState('')
  const [tarefas, setTarefas] = useState([])
  const [tarefa, setTarefa] = useState(tarefaInicial)

  const [editandoId, setEditandoId] = useState(null)
  const [tarefaEdicao, setTarefaEdicao] = useState(tarefaInicial)

  const [carregandoLogin, setCarregandoLogin] = useState(false)
  const [carregandoTarefas, setCarregandoTarefas] = useState(false)
  const [salvandoTarefa, setSalvandoTarefa] = useState(false)
  const [salvandoEdicao, setSalvandoEdicao] = useState(false)

  const [mostrarFormulario, setMostrarFormulario] = useState(false)
  const [erro, setErro] = useState('')
  const [mensagem, setMensagem] = useState('')

  const sair = useCallback(() => {
    sessionStorage.removeItem('gf_token')
    sessionStorage.removeItem('gf_usuario')

    setToken('')
    setUsuario(null)
    setTarefas([])
    setSenha('')
    setErro('')
    setMensagem('')
    setMostrarFormulario(false)
    setEditandoId(null)
  }, [])

  const carregarTarefas = useCallback(async (jwt) => {
    if (!jwt) return

    setCarregandoTarefas(true)
    setErro('')

    try {
      const resposta = await fetch(`${API_URL}/tarefas`, {
        headers: {
          Authorization: `Bearer ${jwt}`,
        },
      })

      if (resposta.status === 401) {
        sair()
        setErro('Sua sessão expirou. Entre novamente.')
        return
      }

      if (!resposta.ok) {
        throw new Error(
          `Não foi possível carregar as tarefas. Código: ${resposta.status}`
        )
      }

      const dados = await resposta.json()
      setTarefas(Array.isArray(dados) ? dados : [])
    } catch (error) {
      setErro(
        error.message ||
          'Não foi possível conectar à API. Verifique se ela está funcionando.'
      )
    } finally {
      setCarregandoTarefas(false)
    }
  }, [sair])

 useEffect(() => {
  if (!usuario || !token) return

  async function buscarTarefas() {
    await carregarTarefas(token)
  }

  buscarTarefas()
}, [usuario, token, carregarTarefas])

  async function entrar(event) {
    event.preventDefault()
    setErro('')
    setMensagem('')
    setCarregandoLogin(true)

    try {
      const resposta = await fetch(`${API_URL}/usuarios/login`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify({
          email: email.trim(),
          senha,
        }),
      })

      const dados = await resposta.json()

      if (!resposta.ok) {
        throw new Error(
          typeof dados === 'string'
            ? dados
            : dados.mensagem || 'E-mail ou senha inválidos.'
        )
      }

      if (!dados.token) {
        throw new Error('A API não retornou um token de autenticação.')
      }

      const dadosUsuario = {
        id: dados.id,
        nome: dados.nome,
        email: dados.email,
        perfil: dados.perfil,
      }

      sessionStorage.setItem('gf_token', dados.token)
      sessionStorage.setItem('gf_usuario', JSON.stringify(dadosUsuario))

      setToken(dados.token)
      setUsuario(dadosUsuario)
      setSenha('')
    } catch (error) {
      setErro(error.message || 'Não foi possível entrar.')
    } finally {
      setCarregandoLogin(false)
    }
  }

  function atualizarCampo(event) {
    const { name, value } = event.target

    setTarefa((atual) => ({
      ...atual,
      [name]: value,
    }))
  }

  function atualizarCampoEdicao(event) {
    const { name, value } = event.target

    setTarefaEdicao((atual) => ({
      ...atual,
      [name]: value,
    }))
  }

  function iniciarEdicao(item) {
    setErro('')
    setMensagem('')
    setMostrarFormulario(false)

    setEditandoId(item.id)
    setTarefaEdicao({
      id: item.id,
      titulo: item.titulo || '',
      descricao: item.descricao || '',
      tipo: item.tipo || 'Sprint',
      responsavel: item.responsavel || '',
      data: dataParaInput(item.data),
      status: item.status || 'Pendente',
    })

    window.scrollTo({ top: 0, behavior: 'smooth' })
  }

  function cancelarEdicao() {
    setEditandoId(null)
    setTarefaEdicao(tarefaInicial)
    setErro('')
  }

  async function cadastrarTarefa(event) {
    event.preventDefault()
    setErro('')
    setMensagem('')
    setSalvandoTarefa(true)

    try {
      const corpo = {
        titulo: tarefa.titulo.trim(),
        descricao: tarefa.descricao.trim(),
        tipo: tarefa.tipo,
        responsavel: tarefa.responsavel.trim(),
        data: `${tarefa.data}T00:00:00`,
        status: tarefa.status,
      }

      const resposta = await fetch(`${API_URL}/tarefas`, {
        method: 'POST',
        headers: {
          Authorization: `Bearer ${token}`,
          'Content-Type': 'application/json',
        },
        body: JSON.stringify(corpo),
      })

      if (resposta.status === 401) {
        sair()
        setErro('Sua sessão expirou. Entre novamente.')
        return
      }

      if (!resposta.ok) {
        const detalhes = await resposta.text()
        throw new Error(
          detalhes || `Não foi possível cadastrar a tarefa (${resposta.status}).`
        )
      }

      setTarefa(tarefaInicial)
      setMostrarFormulario(false)
      setMensagem('Tarefa cadastrada com sucesso!')

      await carregarTarefas(token)
    } catch (error) {
      setErro(error.message || 'Ocorreu um erro ao cadastrar a tarefa.')
    } finally {
      setSalvandoTarefa(false)
    }
  }

  async function salvarEdicao(event) {
    event.preventDefault()
    setErro('')
    setMensagem('')
    setSalvandoEdicao(true)

    try {
      const corpo = {
        id: tarefaEdicao.id,
        titulo: tarefaEdicao.titulo.trim(),
        descricao: tarefaEdicao.descricao.trim(),
        tipo: tarefaEdicao.tipo,
        responsavel: tarefaEdicao.responsavel.trim(),
        data: `${tarefaEdicao.data}T00:00:00`,
        status: tarefaEdicao.status,
      }

      const resposta = await fetch(
        `${API_URL}/tarefas/${editandoId}`,
        {
          method: 'PUT',
          headers: {
            Authorization: `Bearer ${token}`,
            'Content-Type': 'application/json',
          },
          body: JSON.stringify(corpo),
        }
      )

      if (resposta.status === 401) {
        sair()
        setErro('Sua sessão expirou. Entre novamente.')
        return
      }

      if (resposta.status === 403) {
        throw new Error(
          'Você não tem permissão para editar tarefas. A API recusou a alteração (403).'
        )
      }

      if (!resposta.ok) {
        const detalhes = await resposta.text()
        throw new Error(
          detalhes || `Não foi possível editar a tarefa (${resposta.status}).`
        )
      }

      setEditandoId(null)
      setTarefaEdicao(tarefaInicial)
      setMensagem('Tarefa atualizada com sucesso!')

      await carregarTarefas(token)
    } catch (error) {
      setErro(error.message || 'Ocorreu um erro ao editar a tarefa.')
    } finally {
      setSalvandoEdicao(false)
    }
  }

  function formatarData(data) {
    if (!data) return '—'

    const valor = new Date(data)

    if (Number.isNaN(valor.getTime())) return data

    return valor.toLocaleDateString('pt-BR', {
      timeZone: 'UTC',
    })
  }

  if (!usuario || !token) {
    return (
      <main className="login-page">
        <section className="login-card">
          <div className="brand">
            <div className="brand-icon">GF</div>
            <div>
              <h1>GF Gestões</h1>
              <p>Organização e produtividade</p>
            </div>
          </div>

          <div className="login-heading">
            <span className="eyebrow">BEM-VINDO DE VOLTA</span>
            <h2>Entre na sua conta</h2>
            <p>Acesse sua área de gestão de tarefas.</p>
          </div>

          <form onSubmit={entrar} className="login-form">
            <label htmlFor="email">E-mail</label>
            <input
              id="email"
              type="email"
              placeholder="voce@exemplo.com"
              value={email}
              onChange={(event) => setEmail(event.target.value)}
              autoComplete="username"
              required
            />

            <label htmlFor="senha">Senha</label>
            <input
              id="senha"
              type="password"
              placeholder="Digite sua senha"
              value={senha}
              onChange={(event) => setSenha(event.target.value)}
              autoComplete="current-password"
              required
            />

            {erro && <div className="alert error">{erro}</div>}

            <button
              type="submit"
              className="primary-button"
              disabled={carregandoLogin}
            >
              {carregandoLogin ? 'Entrando...' : 'Entrar'}
            </button>
          </form>

          <p className="login-footer">
            Acesso protegido pela autenticação da GF Gestões.
          </p>
        </section>
      </main>
    )
  }

  const pendentes = tarefas.filter((item) =>
    (item.status || '').toLowerCase().includes('pendente')
  ).length

  const emAndamento = tarefas.filter((item) =>
    (item.status || '').toLowerCase().includes('andamento')
  ).length

  return (
    <main className="dashboard">
      <header className="topbar">
        <div className="brand">
          <div className="brand-icon">GF</div>
          <div>
            <h1>GF Gestões</h1>
            <p>Painel de tarefas</p>
          </div>
        </div>

        <div className="user-area">
          <div className="user-info">
            <strong>{usuario.nome || usuario.email}</strong>
            <span>{usuario.perfil || 'Usuario'}</span>
          </div>

          <button className="secondary-button" onClick={sair}>
            Sair
          </button>
        </div>
      </header>

      <section className="dashboard-content">
        <div className="welcome-row">
          <div>
            <span className="eyebrow">VISÃO GERAL</span>
            <h2>Suas tarefas</h2>
            <p>Acompanhe as atividades cadastradas no sistema.</p>
          </div>

          <div className="dashboard-actions">
            <button
              className="secondary-button"
              onClick={() => carregarTarefas(token)}
              disabled={carregandoTarefas}
            >
              {carregandoTarefas ? 'Atualizando...' : 'Atualizar tarefas'}
            </button>

            <button
              className="primary-button"
              onClick={() => {
                cancelarEdicao()
                setMensagem('')
                setMostrarFormulario((atual) => !atual)
              }}
            >
              {mostrarFormulario ? 'Cancelar cadastro' : '+ Nova tarefa'}
            </button>
          </div>
        </div>

        {erro && <div className="alert error">{erro}</div>}
        {mensagem && <div className="alert success">{mensagem}</div>}

        {editandoId !== null && (
          <section className="tasks-card form-card">
            <div className="tasks-heading">
              <h3>Editar tarefa #{editandoId}</h3>
              <p>Altere os campos e salve as modificações.</p>
            </div>

            <form className="task-form" onSubmit={salvarEdicao}>
              <div className="form-field full-width">
                <label htmlFor="editar-titulo">Título</label>
                <input
                  id="editar-titulo"
                  name="titulo"
                  value={tarefaEdicao.titulo}
                  onChange={atualizarCampoEdicao}
                  maxLength={150}
                  required
                />
              </div>

              <div className="form-field full-width">
                <label htmlFor="editar-descricao">Descrição</label>
                <textarea
                  id="editar-descricao"
                  name="descricao"
                  value={tarefaEdicao.descricao}
                  onChange={atualizarCampoEdicao}
                  rows={3}
                  maxLength={2000}
                  required
                />
              </div>

              <div className="form-field">
                <label htmlFor="editar-tipo">Tipo</label>
                <select
                  id="editar-tipo"
                  name="tipo"
                  value={tarefaEdicao.tipo}
                  onChange={atualizarCampoEdicao}
                  required
                >
                  <option value="Backlog">Backlog</option>
                  <option value="Sprint">Sprint</option>
                  <option value="Daily">Daily</option>
                  <option value="Reunião">Reunião</option>
                  <option value="Outro">Outro</option>
                </select>
              </div>

              <div className="form-field">
                <label htmlFor="editar-responsavel">Responsável</label>
                <input
                  id="editar-responsavel"
                  name="responsavel"
                  value={tarefaEdicao.responsavel}
                  onChange={atualizarCampoEdicao}
                  maxLength={100}
                  required
                />
              </div>

              <div className="form-field">
                <label htmlFor="editar-data">Data</label>
                <input
                  id="editar-data"
                  type="date"
                  name="data"
                  value={tarefaEdicao.data}
                  onChange={atualizarCampoEdicao}
                  required
                />
              </div>

              <div className="form-field">
                <label htmlFor="editar-status">Status</label>
                <select
                  id="editar-status"
                  name="status"
                  value={tarefaEdicao.status}
                  onChange={atualizarCampoEdicao}
                  required
                >
                  <option value="Pendente">Pendente</option>
                  <option value="Em andamento">Em andamento</option>
                  <option value="Concluída">Concluída</option>
                  <option value="Concluído">Concluído</option>
                </select>
              </div>

              <div className="form-actions full-width">
                <button
                  type="button"
                  className="secondary-button"
                  onClick={cancelarEdicao}
                  disabled={salvandoEdicao}
                >
                  Cancelar
                </button>

                <button
                  type="submit"
                  className="primary-button"
                  disabled={salvandoEdicao}
                >
                  {salvandoEdicao ? 'Salvando...' : 'Salvar alterações'}
                </button>
              </div>
            </form>
          </section>
        )}

        {mostrarFormulario && (
          <section className="tasks-card form-card">
            <div className="tasks-heading">
              <h3>Cadastrar nova tarefa</h3>
              <p>Preencha os campos para registrar uma atividade.</p>
            </div>

            <form className="task-form" onSubmit={cadastrarTarefa}>
              <div className="form-field full-width">
                <label htmlFor="titulo">Título</label>
                <input
                  id="titulo"
                  name="titulo"
                  value={tarefa.titulo}
                  onChange={atualizarCampo}
                  maxLength={150}
                  required
                />
              </div>

              <div className="form-field full-width">
                <label htmlFor="descricao">Descrição</label>
                <textarea
                  id="descricao"
                  name="descricao"
                  value={tarefa.descricao}
                  onChange={atualizarCampo}
                  rows={3}
                  maxLength={2000}
                  required
                />
              </div>

              <div className="form-field">
                <label htmlFor="tipo">Tipo</label>
                <select
                  id="tipo"
                  name="tipo"
                  value={tarefa.tipo}
                  onChange={atualizarCampo}
                  required
                >
                  <option value="Backlog">Backlog</option>
                  <option value="Sprint">Sprint</option>
                  <option value="Daily">Daily</option>
                  <option value="Reunião">Reunião</option>
                  <option value="Outro">Outro</option>
                </select>
              </div>

              <div className="form-field">
                <label htmlFor="responsavel">Responsável</label>
                <input
                  id="responsavel"
                  name="responsavel"
                  value={tarefa.responsavel}
                  onChange={atualizarCampo}
                  maxLength={100}
                  required
                />
              </div>

              <div className="form-field">
                <label htmlFor="data">Data</label>
                <input
                  id="data"
                  type="date"
                  name="data"
                  value={tarefa.data}
                  onChange={atualizarCampo}
                  required
                />
              </div>

              <div className="form-field">
                <label htmlFor="status">Status</label>
                <select
                  id="status"
                  name="status"
                  value={tarefa.status}
                  onChange={atualizarCampo}
                  required
                >
                  <option value="Pendente">Pendente</option>
                  <option value="Em andamento">Em andamento</option>
                  <option value="Concluída">Concluída</option>
                </select>
              </div>

              <div className="form-actions full-width">
                <button
                  type="button"
                  className="secondary-button"
                  onClick={() => {
                    setMostrarFormulario(false)
                    setTarefa(tarefaInicial)
                    setErro('')
                  }}
                  disabled={salvandoTarefa}
                >
                  Cancelar
                </button>

                <button
                  type="submit"
                  className="primary-button"
                  disabled={salvandoTarefa}
                >
                  {salvandoTarefa ? 'Salvando...' : 'Salvar tarefa'}
                </button>
              </div>
            </form>
          </section>
        )}

        <div className="stats-grid">
          <article className="stat-card">
            <span>Total de tarefas</span>
            <strong>{tarefas.length}</strong>
          </article>

          <article className="stat-card">
            <span>Pendentes</span>
            <strong>{pendentes}</strong>
          </article>

          <article className="stat-card">
            <span>Em andamento</span>
            <strong>{emAndamento}</strong>
          </article>
        </div>

        <section className="tasks-card">
          <div className="tasks-heading">
            <h3>Lista de tarefas</h3>
            <p>Dados carregados da API GF Gestões.</p>
          </div>

          {carregandoTarefas && tarefas.length === 0 ? (
            <p className="empty-state">Carregando tarefas...</p>
          ) : tarefas.length === 0 ? (
            <p className="empty-state">
              Nenhuma tarefa cadastrada ou disponível.
            </p>
          ) : (
            <div className="table-wrapper">
              <table>
                <thead>
                  <tr>
                    <th>Tarefa</th>
                    <th>Tipo</th>
                    <th>Responsável</th>
                    <th>Data</th>
                    <th>Status</th>
                    <th>Ações</th>
                  </tr>
                </thead>

                <tbody>
                  {tarefas.map((item) => (
                    <tr key={item.id}>
                      <td>
                        <strong>{item.titulo}</strong>
                        <span className="task-description">
                          {item.descricao}
                        </span>
                      </td>
                      <td>{item.tipo || '—'}</td>
                      <td>{item.responsavel || '—'}</td>
                      <td>{formatarData(item.data)}</td>
                      <td>
                        <span className="status-badge">
                          {item.status || 'Sem status'}
                        </span>
                      </td>
                      <td>
                        <button
                          type="button"
                          className="secondary-button edit-button"
                          onClick={() => iniciarEdicao(item)}
                        >
                          Editar
                        </button>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
        </section>
      </section>
    </main>
  )
}

export default App
